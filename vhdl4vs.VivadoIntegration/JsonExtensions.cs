using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace vhdl4vs.VivadoIntegration
{
	internal static class JsonExtensions
	{
		public static JsonElement? GetProperty2(this JsonElement e, string propertyName)
		{
			if (!e.TryGetProperty(propertyName, out var property))
				return null;
			return property;
		}
	}
}
