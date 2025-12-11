using System;
using System.Collections.Generic;

namespace GbService.Model.Common
{
	public abstract class AuditBase
	{
		public string AuditUserName { get; set; }

		public DateTime AuditDate { get; set; }

		public string AuditAction { get; set; }

		public string ChangedColumns { get; set; }

		public string AuditActionTxt
		{
			get
			{
				return this.actions.ContainsKey(this.AuditAction) ? this.actions[this.AuditAction] : "";
			}
		}

		private readonly Dictionary<string, string> actions = new Dictionary<string, string>
		{
			{
				"Insert",
				"Ajout"
			},
			{
				"Update",
				"Modification"
			},
			{
				"Delete",
				"Supprission"
			}
		};
	}
}
