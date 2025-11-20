namespace Binah.Ontology.Models.Relationship;

/// <summary>
    /// HAS_PERMIT relationship: Property has associated Document (permit)
    /// Direction: Property -> Document
    /// </summary>
    public class HasPermitRelationship : Relationship
    {
        public HasPermitRelationship()
        {
            Type = "HAS_PERMIT";
        }

        /// <summary>Permit status: open, closed</summary>
        public string? PermitStatus
        {
            get => GetPropertyValue<string>("permit_status");
            set => SetPropertyValue("permit_status", value);
        }
    }