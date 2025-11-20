namespace Binah.Ontology.Models.Relationship;

/// <summary>
    /// DEVELOPED_BY relationship: Property was developed by Entity
    /// Direction: Property -> Entity
    /// </summary>
    public class DevelopedByRelationship : Relationship
    {
        public DevelopedByRelationship()
        {
            Type = "DEVELOPED_BY";
        }

        /// <summary>Development project name</summary>
        public string? ProjectName
        {
            get => GetPropertyValue<string>("project_name");
            set => SetPropertyValue("project_name", value);
        }

        /// <summary>Project completion date</summary>
        public DateTime? CompletionDate
        {
            get => GetPropertyValue<DateTime?>("completion_date");
            set => SetPropertyValue("completion_date", value);
        }
    }