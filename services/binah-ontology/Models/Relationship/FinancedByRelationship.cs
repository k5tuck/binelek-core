namespace Binah.Ontology.Models.Relationship;

/// <summary>
    /// FINANCED_BY relationship: Property is financed by Entity
    /// Direction: Property -> Entity
    /// </summary>
    public class FinancedByRelationship : Relationship
    {
        public FinancedByRelationship()
        {
            Type = "FINANCED_BY";
        }

        /// <summary>Loan amount</summary>
        public decimal? LoanAmount
        {
            get => GetPropertyValue<decimal?>("loan_amount");
            set => SetPropertyValue("loan_amount", value);
        }

        /// <summary>Interest rate</summary>
        public double? Rate
        {
            get => GetPropertyValue<double?>("rate");
            set => SetPropertyValue("rate", value);
        }

        /// <summary>Lien position (1st, 2nd, etc.)</summary>
        public int? LienPosition
        {
            get => GetPropertyValue<int?>("lien_position");
            set => SetPropertyValue("lien_position", value);
        }

        /// <summary>Loan maturity date</summary>
        public DateTime? MaturityDate
        {
            get => GetPropertyValue<DateTime?>("maturity_date");
            set => SetPropertyValue("maturity_date", value);
        }
    }