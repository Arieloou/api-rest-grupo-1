using System.ComponentModel.DataAnnotations;
using GestionPedidos.Models.Requests.Customers;
using GestionPedidos.Models.Responses;
using GestionPedidos.Models.Results;
using GestionPedidos.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestionPedidos.Controllers;

[ApiController]
[Route("customers")]
[Produces("application/json")]
public sealed class CustomersController(
    CustomerService customerService,
    OrderService orderService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<CustomerResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyCollection<CustomerResponse>> GetAll() =>
        Ok(customerService.GetAll());

    [HttpGet("{customerId}")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public ActionResult<CustomerResponse> GetById(
        [FromRoute]
        [RegularExpression("^C-[0-9]+$", ErrorMessage = "customerId must use the format C- followed by digits.")]
        string customerId)
    {
        var customer = customerService.GetById(customerId);
        if (customer is null)
        {
            return NotFound(Error("CUSTOMER_NOT_FOUND", $"Customer {customerId} was not found."));
        }

        return Ok(customer);
    }

    [HttpPost]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public ActionResult<CustomerResponse> Create([FromBody] CreateCustomerRequest request)
    {
        var result = customerService.Create(request);
        if (result.Failure == CustomerOperationFailure.DuplicateEmail)
        {
            return Conflict(Error(
                "CUSTOMER_EMAIL_ALREADY_EXISTS",
                $"A customer with email {request.Email.Trim().ToLowerInvariant()} already exists."));
        }

        return CreatedAtAction(
            nameof(GetById),
            new { customerId = result.Customer!.Id },
            result.Customer);
    }

    [HttpPut("{customerId}")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public ActionResult<CustomerResponse> Update(
        [FromRoute]
        [RegularExpression("^C-[0-9]+$", ErrorMessage = "customerId must use the format C- followed by digits.")]
        string customerId,
        [FromBody] UpdateCustomerRequest request)
    {
        var result = customerService.Update(customerId, request);
        return result.Failure switch
        {
            CustomerOperationFailure.None => Ok(result.Customer),
            CustomerOperationFailure.NotFound =>
                NotFound(Error("CUSTOMER_NOT_FOUND", $"Customer {customerId} was not found.")),
            CustomerOperationFailure.DuplicateEmail =>
                Conflict(Error(
                    "CUSTOMER_EMAIL_ALREADY_EXISTS",
                    $"A customer with email {request.Email.Trim().ToLowerInvariant()} already exists.")),
            _ => throw new InvalidOperationException("Unsupported customer operation failure.")
        };
    }

    [HttpDelete("{customerId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public IActionResult Delete(
        [FromRoute]
        [RegularExpression("^C-[0-9]+$", ErrorMessage = "customerId must use the format C- followed by digits.")]
        string customerId)
    {
        if (customerService.GetById(customerId) is null)
        {
            return NotFound(Error("CUSTOMER_NOT_FOUND", $"Customer {customerId} was not found."));
        }

        if (orderService.HasOrdersForCustomer(customerId))
        {
            return Conflict(Error(
                "CUSTOMER_HAS_ORDERS",
                $"Customer {customerId} cannot be deleted because it has orders."));
        }

        customerService.Delete(customerId);
        return NoContent();
    }

    private ErrorResponse Error(string code, string message) =>
        new(code, message, HttpContext.TraceIdentifier);
}
