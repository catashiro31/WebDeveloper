using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using WebDeveloper.Helpers;

void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
void Reject(Action action)
{
    try { action(); } catch (InvalidOperationException) { return; }
    throw new Exception("Invalid relationship was accepted");
}
Check(PatientRelationship.Validate(" Mẹ ") == "Mẹ", "Normalize relationship");
Check(PatientRelationship.Validate("SELF", "SELF") == "SELF", "Preserve self");
Check(PatientRelationship.Validate("Cô", "Cô") == "Cô", "Preserve existing legacy label");
Reject(() => PatientRelationship.Validate("SELF"));
Reject(() => PatientRelationship.Validate("Mẹ", "SELF"));
Reject(() => PatientRelationship.Validate("tùy ý"));
Reject(() => PatientRelationship.Validate(""));
var state = new ModelStateDictionary();
state.AddModelError("email", "The JSON value could not be converted to System.String.");
state.AddModelError("number", "The value 'Mẹ' is not valid.");
state.AddModelError("fullName", "Họ tên không được để trống");
var context = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor(), state);
var result = (BadRequestObjectResult)ApiValidationMessages.InvalidRequest(context);
var problem = (ValidationProblemDetails)result.Value!;
Check(result.StatusCode == 400, "Keep bad request status");
Check(problem.Errors["email"][0].StartsWith("Thông tin chưa hợp lệ"), "Translate binding errors");
Check(problem.Errors["number"][0].StartsWith("Thông tin chưa hợp lệ"), "Translate binding errors containing Vietnamese input");
Check(problem.Errors["fullName"][0] == "Họ tên không được để trống", "Preserve specific Vietnamese validation");
Console.WriteLine("PASS: relationship validation, self protection, legacy compatibility and Vietnamese API errors.");
