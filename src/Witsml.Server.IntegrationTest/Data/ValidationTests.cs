using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Framework;

namespace PDS.Witsml.Server.Data
{
    [TestClass]
    public class ValidationTests
    {
        [Ignore]
        [TestMethod]
        public void Validate_wellbore_properties()
        {
            IList<ValidationResult> results;

            var entity = new Wellbore()
            {
                NameWell = "Well 01",
                Name = "Wellbore 01-01"
            };

            var result = EntityValidator.TryValidate(new TestValidator(entity), out results);

            Assert.IsTrue(result);
        }

        public class TestValidator : IValidatableObject
        {
            public TestValidator(Wellbore wellbore)
            {
            }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                yield return new ValidationResult("UidWell is required.", new[] { "UidWell " });
            }
        }
    }
}
