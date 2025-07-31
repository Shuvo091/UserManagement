using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CohesionX.Usermanagement.Test;
public static class ModelValidator
{
	public static IList<ValidationResult> Validate(object model)
	{
		var results = new List<ValidationResult>();
		var context = new ValidationContext(model, null, null);
		Validator.TryValidateObject(model, context, results, validateAllProperties: true);
		return results;
	}
}
