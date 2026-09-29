using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Admin;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/contracts")]
public class AdminEmploymentContractController : ControllerBase
{
    private readonly IAdminEmploymentContractService _contractService;
    private readonly ILogger<AdminEmploymentContractController> _logger;

    public AdminEmploymentContractController(
        IAdminEmploymentContractService contractService,
        ILogger<AdminEmploymentContractController> logger)
    {
        _contractService = contractService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paged list of employment contracts for admin with optional filtering and search.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AdminEmploymentContractListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResponse<AdminEmploymentContractListItemResponse>>> GetContracts(
        [FromQuery] AdminEmploymentContractListRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _contractService.GetPagedAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while retrieving employment contracts.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Employment contracts could not be retrieved. Please try again later." });
        }
    }

    /// <summary>
    /// Retrieves detailed information for a specific employment contract ID.
    /// </summary>
    [HttpGet("{contractId:int}")]
    [ProducesResponseType(typeof(AdminEmploymentContractDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AdminEmploymentContractDetailResponse>> GetContractDetail(
        int contractId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _contractService.GetByIdAsync(contractId, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while retrieving employment contract {ContractId}", contractId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "The employment contract could not be retrieved. Please try again later." });
        }
    }
}
