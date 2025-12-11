using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace GbService.Model.Common
{
	public class EntityBase : INotifyPropertyChanged
	{
		public string Error
		{
			get
			{
				return "";
			}
		}

		//[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public event PropertyChangedEventHandler PropertyChanged;

		protected void RaisePropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
			if (propertyChanged != null)
			{
				propertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		protected bool Set<T>(ref T field, T value)
		{
			bool flag = EqualityComparer<T>.Default.Equals(field, value);
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				field = value;
				result = true;
			}
			return result;
		}
	}
}
