using EcommercePlatform.Application.Features.Listings.Commands;
        using EcommercePlatform.Application.Features.Listings.Handlers;
        using EcommercePlatform.Application.Features.Listings.Queries; // For ListingItemViewModel
        using Microsoft.AspNetCore.Mvc;
        using System.Net.Mime;
        using Microsoft.AspNetCore.Http;
        using System; // For ArgumentNullException, ApplicationException
        using System.Threading; // For CancellationToken
        using System.Threading.Tasks; // For Task
        using Microsoft.Extensions.Logging; // For ILogger

        namespace EcommercePlatform.API.Controllers;

        [ApiController]
        [Route("api/[controller]")]
        [Produces(MediaTypeNames.Application.Json)]
        public class ListingsController : ControllerBase
        {
            private readonly CreateListingCommandHandler _createListingHandler;
            private readonly GetListingByIdQueryHandler _getListingByIdHandler;
            private readonly ILogger<ListingsController> _logger;

            public ListingsController(
                CreateListingCommandHandler createListingHandler,
                GetListingByIdQueryHandler getListingByIdHandler,
                ILogger<ListingsController> logger)
            {
                _createListingHandler = createListingHandler ?? throw new ArgumentNullException(nameof(createListingHandler));
                _getListingByIdHandler = getListingByIdHandler ?? throw new ArgumentNullException(nameof(getListingByIdHandler));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            /// <summary>
            /// Creates a new listing.
            /// </summary>
            [HttpPost]
            [Consumes(MediaTypeNames.Application.Json)]
            [ProducesResponseType(typeof(object), StatusCodes.Status201Created)] // Returning { id = listingId }
            [ProducesResponseType(StatusCodes.Status400BadRequest)]
            [ProducesResponseType(StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> CreateListing([FromBody] CreateListingCommand command, CancellationToken cancellationToken)
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("CreateListing called with invalid model state: {@ModelState}", ModelState.Values.SelectMany(v => v.Errors));
                    return BadRequest(ModelState);
                }
                try
                {
                    // SellerId should ideally be set from authenticated user context in a real app
                    // For now, we allow it in the command for testing.
                    var listingId = await _createListingHandler.HandleAsync(command, cancellationToken);
                    _logger.LogInformation("Listing created successfully with ID: {ListingId}", listingId);
                    return CreatedAtAction(nameof(GetListingById), new { id = listingId }, new { id = listingId });
                }
                catch (ArgumentException ex) // Catch specific validation/argument errors from handler
                {
                    _logger.LogWarning(ex, "Argument error during listing creation: {ErrorMessage}", ex.Message);
                    return BadRequest(new { error = ex.Message });
                }
                catch (ApplicationException ex) // Catch application-specific errors (e.g., Seller not found)
                {
                     _logger.LogWarning(ex, "Application error during listing creation: {ErrorMessage}", ex.Message);
                    return BadRequest(new { error = ex.Message });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unexpected error occurred while creating listing: {@Command}", command);
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
                }
            }

            /// <summary>
            /// Gets a specific listing by its ID.
            /// </summary>
            [HttpGet("{id}")]
            [ProducesResponseType(typeof(ListingItemViewModel), StatusCodes.Status200OK)]
            [ProducesResponseType(StatusCodes.Status404NotFound)]
            [ProducesResponseType(StatusCodes.Status400BadRequest)]
            public async Task<ActionResult<ListingItemViewModel>> GetListingById(string id, CancellationToken cancellationToken)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("GetListingById called with empty ID.");
                    return BadRequest(new { error = "Listing ID cannot be empty."});
                }
                var query = new GetListingByIdQuery { Id = id };
                var listingViewModel = await _getListingByIdHandler.HandleAsync(query, cancellationToken);
                if (listingViewModel == null)
                {
                    _logger.LogInformation("Listing with ID {ListingId} not found.", id);
                    return NotFound(new { message = $"Listing with ID {id} not found." });
                }
                _logger.LogInformation("Listing with ID {ListingId} retrieved successfully.", id);
                return Ok(listingViewModel);
            }
        }