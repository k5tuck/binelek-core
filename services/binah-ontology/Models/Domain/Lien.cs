using Binah.Core.Exceptions;
using Binah.Ontology.Models.Base;
using Binah.Ontology.Models.SupportModels;
namespace Binah.Ontology.Models.Domain;

/// <summary>
    /// Represents a lien on a property
    /// Label: Lien
    /// </summary>
    public class Lien : Entity
    {
        public Lien()
        {
            Type = "Lien";
        }

        /// <summary>Canonical lien identifier (UUID)</summary>
        public string Uid
        {
            get => Id;
            set => Id = value;
        }

        /// <summary>Lien type: tax, mechanic, judgment, mortgage, hoa, irs, child_support</summary>
        public string? LienType
        {
            get => GetPropertyValue<string>("lien_type");
            set => SetPropertyValue("lien_type", value);
        }

        /// <summary>Official lien number or recording number</summary>
        public string? LienNumber
        {
            get => GetPropertyValue<string>("lien_number");
            set => SetPropertyValue("lien_number", value);
        }

        /// <summary>Lien amount</summary>
        public decimal? Amount
        {
            get => GetPropertyValue<decimal?>("amount");
            set => SetPropertyValue("amount", value);
        }

        /// <summary>Date lien was filed/recorded</summary>
        public DateTime? FiledDate
        {
            get => GetPropertyValue<DateTime?>("filed_date");
            set => SetPropertyValue("filed_date", value);
        }

        /// <summary>Date lien was released (if applicable)</summary>
        public DateTime? ReleasedDate
        {
            get => GetPropertyValue<DateTime?>("released_date");
            set => SetPropertyValue("released_date", value);
        }

        /// <summary>Lien status: active, released, satisfied, foreclosed</summary>
        public string? Status
        {
            get => GetPropertyValue<string>("status");
            set => SetPropertyValue("status", value);
        }

        /// <summary>Lien priority/position (1st, 2nd, etc.)</summary>
        public int? Priority
        {
            get => GetPropertyValue<int?>("priority");
            set => SetPropertyValue("priority", value);
        }

        /// <summary>Entity ID of lien holder</summary>
        public string? LienHolderId
        {
            get => GetPropertyValue<string>("lien_holder_id");
            set => SetPropertyValue("lien_holder_id", value);
        }

        /// <summary>Recording jurisdiction (county, state)</summary>
        public string? Jurisdiction
        {
            get => GetPropertyValue<string>("jurisdiction");
            set => SetPropertyValue("jurisdiction", value);
        }

        /// <summary>Description or reason for lien</summary>
        public string? Description
        {
            get => GetPropertyValue<string>("description");
            set => SetPropertyValue("description", value);
        }

        /// <summary>Source records for data provenance</summary>
        public List<SourceRecord>? SourceRecords
        {
            get => GetPropertyValue<List<SourceRecord>>("source_records");
            set => SetPropertyValue("source_records", value);
        }
    }