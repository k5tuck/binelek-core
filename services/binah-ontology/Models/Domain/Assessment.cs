using Binah.Core.Exceptions;
using Binah.Ontology.Models.Base;
using Binah.Ontology.Models.SupportModels;
namespace Binah.Ontology.Models.Domain;

/// <summary>
    /// Represents a property tax assessment
    /// Label: Assessment
    /// </summary>
    public class Assessment : Entity
    {
        public Assessment()
        {
            Type = "Assessment";
        }

        /// <summary>Canonical assessment identifier (UUID)</summary>
        public string Uid
        {
            get => Id;
            set => Id = value;
        }

        /// <summary>Tax year for this assessment</summary>
        public int TaxYear
        {
            get => GetPropertyValue<int>("tax_year");
            set => SetPropertyValue("tax_year", value);
        }

        /// <summary>Assessed land value</summary>
        public decimal? LandValue
        {
            get => GetPropertyValue<decimal?>("land_value");
            set => SetPropertyValue("land_value", value);
        }

        /// <summary>Assessed improvement/building value</summary>
        public decimal? ImprovementValue
        {
            get => GetPropertyValue<decimal?>("improvement_value");
            set => SetPropertyValue("improvement_value", value);
        }

        /// <summary>Total assessed value (land + improvements)</summary>
        public decimal? TotalValue
        {
            get => GetPropertyValue<decimal?>("total_value");
            set => SetPropertyValue("total_value", value);
        }

        /// <summary>Market value estimate</summary>
        public decimal? MarketValue
        {
            get => GetPropertyValue<decimal?>("market_value");
            set => SetPropertyValue("market_value", value);
        }

        /// <summary>Taxable value (may differ from assessed due to exemptions)</summary>
        public decimal? TaxableValue
        {
            get => GetPropertyValue<decimal?>("taxable_value");
            set => SetPropertyValue("taxable_value", value);
        }

        /// <summary>Annual property tax amount</summary>
        public decimal? TaxAmount
        {
            get => GetPropertyValue<decimal?>("tax_amount");
            set => SetPropertyValue("tax_amount", value);
        }

        /// <summary>Effective tax rate</summary>
        public double? TaxRate
        {
            get => GetPropertyValue<double?>("tax_rate");
            set => SetPropertyValue("tax_rate", value);
        }

        /// <summary>Date assessment was issued</summary>
        public DateTime? AssessmentDate
        {
            get => GetPropertyValue<DateTime?>("assessment_date");
            set => SetPropertyValue("assessment_date", value);
        }

        /// <summary>Exemptions applied (homestead, senior, veteran, etc.)</summary>
        public List<string>? Exemptions
        {
            get => GetPropertyValue<List<string>>("exemptions");
            set => SetPropertyValue("exemptions", value);
        }

        /// <summary>Exemption amount</summary>
        public decimal? ExemptionAmount
        {
            get => GetPropertyValue<decimal?>("exemption_amount");
            set => SetPropertyValue("exemption_amount", value);
        }

        /// <summary>Assessment status: preliminary, final, appealed</summary>
        public string? Status
        {
            get => GetPropertyValue<string>("status");
            set => SetPropertyValue("status", value);
        }

        /// <summary>Assessing jurisdiction</summary>
        public string? Jurisdiction
        {
            get => GetPropertyValue<string>("jurisdiction");
            set => SetPropertyValue("jurisdiction", value);
        }

        /// <summary>Change from previous year assessment</summary>
        public decimal? YearOverYearChange
        {
            get => GetPropertyValue<decimal?>("year_over_year_change");
            set => SetPropertyValue("year_over_year_change", value);
        }

        /// <summary>Source records for data provenance</summary>
        public List<SourceRecord>? SourceRecords
        {
            get => GetPropertyValue<List<SourceRecord>>("source_records");
            set => SetPropertyValue("source_records", value);
        }
    }