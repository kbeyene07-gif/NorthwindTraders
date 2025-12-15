using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindTraders.Api.Security;
using NorthwindTraders.Application.Dtos.Products;
using NorthwindTraders.Application.Services.Products;


namespace NorthwindTraders.Api.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = AuthScopes.ProductsRead)]   // All actions need read:products
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _service;

        public ProductsController(IProductService service)
        {
            _service = service;
        }

        // GET: api/v1/products?pageNumber=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> GetProducts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            var result = await _service.GetPagedAsync(pageNumber, pageSize, ct);
            return Ok(result);
        }

        // GET: api/v1/products/3
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProduct(int id, CancellationToken ct = default)
        {
            var product = await _service.GetByIdAsync(id, ct);
            if (product == null)
                return NotFound();

            return Ok(product);
        }

        // POST: api/v1/products
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.ProductsWriteOrAdmin)]
        public async Task<IActionResult> CreateProduct(
            [FromBody] CreateProductDto dto,
            CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var created = await _service.CreateAsync(dto, ct);

            return CreatedAtAction(
                nameof(GetProduct),
                new { id = created.Id, version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0" },
                created);
        }

        // PUT: api/v1/products/3
        [HttpPut("{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.ProductsWriteOrAdmin)]
        public async Task<IActionResult> UpdateProduct(
            int id,
            [FromBody] UpdateProductDto dto,
            CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var success = await _service.UpdateAsync(id, dto, ct);
            if (!success)
                return NotFound();

            return NoContent();
        }

        // DELETE: api/v1/products/3
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id, CancellationToken ct = default)
        {
            var success = await _service.DeleteAsync(id, ct);
            if (!success)
                return NotFound();

            return NoContent();
        }
    }
}
