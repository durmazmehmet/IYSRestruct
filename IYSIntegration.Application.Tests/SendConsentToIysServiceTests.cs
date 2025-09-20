using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IYSIntegration.Application.Services;
using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Response.Consent;
using IYSIntegration.Application.Services.Models.Response.Schedule;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IYSIntegration.Application.Tests
{
    public class SendConsentToIysServiceTests
    {
        [Fact]
        public async Task RunAsync_CallsUpdateConsentResponses_WhenSuccessfulResponseExists()
        {
            var loggerMock = new Mock<ILogger<SendConsentToIysService>>();
            var dbServiceMock = new Mock<IDbService>();
            var helperMock = new Mock<IIysHelper>();
            var proxyMock = new Mock<IIysProxy>();
            var configurationMock = new Mock<IConfiguration>();

            var consentLog = new ConsentRequestLog
            {
                Id = 1,
                CompanyCode = "COMP1",
                IysCode = 100,
                BrandCode = 200,
                ConsentDate = DateTimeOffset.UtcNow.ToString("o"),
                Recipient = "test@example.com",
                RecipientType = "BIREYSEL",
                Source = "TEST",
                Status = "ON",
                Type = "EPOSTA"
            };

            dbServiceMock.Setup(x => x.GetPendingConsents(It.IsAny<int>()))
                .ReturnsAsync(new List<ConsentRequestLog> { consentLog });

            dbServiceMock.Setup(x => x.UpdateConsentResponses(It.IsAny<IEnumerable<ConsentResponseUpdate>>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            proxyMock.Setup(x => x.PostJsonAsync<RecipientKeyWithList, MultipleQueryConsentResult>(
                    It.IsAny<string>(),
                    It.IsAny<RecipientKeyWithList>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResponseBase<MultipleQueryConsentResult>
                {
                    HttpStatusCode = 200,
                    Status = ServiceResponseStatuses.Success,
                    Data = new MultipleQueryConsentResult
                    {
                        List = new List<string>()
                    }
                });

            proxyMock.Setup(x => x.PostJsonAsync<Consent, AddConsentResult>(
                    It.IsAny<string>(),
                    It.IsAny<Consent>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResponseBase<AddConsentResult>
                {
                    HttpStatusCode = 200,
                    Status = ServiceResponseStatuses.Success,
                    Id = consentLog.Id,
                    LogId = 10,
                    Data = new AddConsentResult
                    {
                        TransactionId = "txn",
                        CreationDate = "2024-01-01T00:00:00Z"
                    }
                });

            var service = new SendConsentToIysService(
                loggerMock.Object,
                dbServiceMock.Object,
                helperMock.Object,
                proxyMock.Object,
                configurationMock.Object);

            await service.RunAsync(5);

            dbServiceMock.Verify(x => x.UpdateConsentResponses(It.Is<IEnumerable<ConsentResponseUpdate>>(updates =>
                updates.Any(update => update.IsSuccess && update.BatchError == null && !update.IsOverdue))), Times.Once);
        }

        [Fact]
        public async Task RunAsync_MarksConsentAsOverdue_WhenApprovalAlreadyExists()
        {
            var loggerMock = new Mock<ILogger<SendConsentToIysService>>();
            var dbServiceMock = new Mock<IDbService>();
            var helperMock = new Mock<IIysHelper>();
            var proxyMock = new Mock<IIysProxy>();
            var configurationMock = new Mock<IConfiguration>();

            var consentLog = new ConsentRequestLog
            {
                Id = 2,
                LogId = 15,
                CompanyCode = "COMP1",
                IysCode = 100,
                BrandCode = 200,
                ConsentDate = DateTimeOffset.UtcNow.ToString("o"),
                Recipient = "existing@example.com",
                RecipientType = "BIREYSEL",
                Source = "TEST",
                Status = "ON",
                Type = "EPOSTA"
            };

            dbServiceMock.Setup(x => x.GetPendingConsents(It.IsAny<int>()))
                .ReturnsAsync(new List<ConsentRequestLog> { consentLog });

            dbServiceMock.Setup(x => x.UpdateConsentResponses(It.IsAny<IEnumerable<ConsentResponseUpdate>>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            proxyMock.Setup(x => x.PostJsonAsync<RecipientKeyWithList, MultipleQueryConsentResult>(
                    It.IsAny<string>(),
                    It.IsAny<RecipientKeyWithList>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResponseBase<MultipleQueryConsentResult>
                {
                    HttpStatusCode = 200,
                    Status = ServiceResponseStatuses.Success,
                    Data = new MultipleQueryConsentResult
                    {
                        List = new List<string> { "existing@example.com" }
                    }
                });

            var service = new SendConsentToIysService(
                loggerMock.Object,
                dbServiceMock.Object,
                helperMock.Object,
                proxyMock.Object,
                configurationMock.Object);

            await service.RunAsync(5);

            proxyMock.Verify(x => x.PostJsonAsync<Consent, AddConsentResult>(
                It.IsAny<string>(),
                It.IsAny<Consent>(),
                It.IsAny<CancellationToken>()), Times.Never);

            dbServiceMock.Verify(x => x.UpdateConsentResponses(It.Is<IEnumerable<ConsentResponseUpdate>>(updates =>
                updates.Any(update =>
                    !update.IsSuccess &&
                    update.IsOverdue &&
                    update.BatchError == "Rıza zaten IYS listesinde mevcut."))), Times.Once);
        }
    }
}
