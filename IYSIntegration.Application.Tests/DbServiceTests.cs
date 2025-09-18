using System.Collections.Generic;
using IYSIntegration.Application.Services;
using IYSIntegration.Application.Services.Models.Base;
using Xunit;

namespace IYSIntegration.Application.Tests
{
    public class DbServiceTests
    {
        [Fact]
        public void BuildDuplicateCleanupCandidates_ReturnsPerChannelEntries()
        {
            var consents = new List<Consent>
            {
                new Consent { CompanyCode = "C1", Recipient = "recipient", RecipientType = "BIREYSEL", Type = "EPOSTA", Status = "ON" },
                new Consent { CompanyCode = "C1", Recipient = "recipient", RecipientType = "BIREYSEL", Type = "ARAMA", Status = "ON" },
                new Consent { CompanyCode = "C1", Recipient = "recipient", RecipientType = "BIREYSEL", Type = "MESAJ", Status = "ON" }
            };

            var candidates = DbService.BuildDuplicateCleanupCandidates(consents);

            Assert.Equal(3, candidates.Count);
            Assert.Contains(candidates, c => c.Type == "EPOSTA");
            Assert.Contains(candidates, c => c.Type == "ARAMA");
            Assert.Contains(candidates, c => c.Type == "MESAJ");
            Assert.All(candidates, c => Assert.Equal("C1", c.CompanyCode));
            Assert.All(candidates, c => Assert.Equal("recipient", c.Recipient));
            Assert.All(candidates, c => Assert.Equal("BIREYSEL", c.RecipientType));
            Assert.All(candidates, c => Assert.Equal("ON", c.Status));
        }

        [Fact]
        public void BuildDuplicateCleanupCandidates_DeduplicatesByStatus()
        {
            var consents = new List<Consent>
            {
                new Consent { CompanyCode = "C1", Recipient = "recipient", RecipientType = "BIREYSEL", Type = "EPOSTA", Status = "ON" },
                new Consent { CompanyCode = "C1", Recipient = "recipient", RecipientType = "BIREYSEL", Type = "EPOSTA", Status = "ON" },
                new Consent { CompanyCode = "C1", Recipient = "recipient", RecipientType = "BIREYSEL", Type = "EPOSTA", Status = "OFF" }
            };

            var candidates = DbService.BuildDuplicateCleanupCandidates(consents);

            Assert.Equal(2, candidates.Count);
            Assert.Contains(candidates, c => c.Status == "ON");
            Assert.Contains(candidates, c => c.Status == "OFF");
        }
    }
}
