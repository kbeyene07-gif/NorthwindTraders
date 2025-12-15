using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindTraders.Application.Dtos.Suppliers;
using NorthwindTraders.Application.Services.Suppliers;


namespace NorthwindTraders.Api.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Roles = "Admin")]
    public class SuppliersController : ControllerBase
    {
        private readonly ISupplierService _service;

        public SuppliersController(ISupplierService service)
        {
            _service = service;
        }

        // GET: api/v1/suppliers?pageNumber=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> GetSuppliers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            var result = await _service.GetPagedAsync(pageNumber, pageSize, ct);
            return Ok(result);
        }

        // GET: api/v1/suppliers/2
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSupplier(int id, CancellationToken ct = default)
        {
            var supplier = await _service.GetByIdAsync(id, ct);
            if (supplier == null)
                return NotFound();

            return Ok(supplier);
        }

        // POST: api/v1/suppliers
        [HttpPost]
        public async Task<IActionResult> CreateSupplier(
            [FromBody] CreateSupplierDto dto,
            CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var created = await _service.CreateAsync(dto, ct);

            return CreatedAtAction(
                nameof(GetSupplier),
                new { id = created.Id, version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0" },
                created);
        }

        // PUT: api/v1/suppliers/2
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateSupplier(
            int id,
            [FromBody] UpdateSupplierDto dto,
            CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var success = await _service.UpdateAsync(id, dto, ct);
            if (!success)
                return NotFound();

            return NoContent();
        }

        // DELETE: api/v1/suppliers/2
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteSupplier(int id, CancellationToken ct = default)
        {
            var success = await _service.DeleteAsync(id, ct);
            if (!success)
                return NotFound();

            return NoContent();
        }
    }
}
