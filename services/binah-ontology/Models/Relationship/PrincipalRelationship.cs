namespace Binah.Ontology.Models.Relationship;

/// <summary>
    /// PRINCIPAL relationship: Person is principal of Entity
    /// Direction: Entity -> Person
    /// </summary>
    public class PrincipalRelationship : Relationship
    {
        public PrincipalRelationship()
        {
            Type = "PRINCIPAL";
        }

        /// <summary>Role in the entity</summary>
        public string? Role
        {
            get => GetPropertyValue<string>("role");
            set => SetPropertyValue("role", value);
        }

        /// <summary>Date when appointed to role</summary>
        public DateTime? AppointedDate
        {
            get => GetPropertyValue<DateTime?>("appointed_date");
            set => SetPropertyValue("appointed_date", value);
        }
    }
