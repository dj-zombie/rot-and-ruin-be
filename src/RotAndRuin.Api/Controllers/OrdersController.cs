using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RotAndRuin.Application.DTOs;
using RotAndRuin.Application.Services;
using RotAndRuin.Application.Interfaces;
using RotAndRuin.Application.DTOs.Requests;
using Swashbuckle.AspNetCore.Annotations;

namespace RotAndRuin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    private readonly IOrderService _orderService = orderService;
    private const string CartSessionKey = "CartId";

    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderRequest request)
    {
        var sessionId = HttpContext.Session.GetString(CartSessionKey);
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(sessionId))
        {
            return BadRequest("No active cart session found");
        }
        
        try 
        {
            return await _orderService.CreateOrderFromCartAsync(sessionId, userId ,request.ShippingAddress);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    [Authorize]
    // public async Task<ActionResult<List<OrderDto>>> GetUserOrders()
    public async Task<ActionResult<List<OrderDto>>> GetOrders()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.IsInRole("Admin");
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        try
        {
            if (isAdmin)
            {
                // Admin can see all orders
                return await _orderService.GetAllOrdersAsync();
            }
            else
            {
                // Regular users can only see their own orders
                return await _orderService.GetUserOrdersAsync(userId);
            }
        }
        catch (Exception ex)
        {
            // Add logging here
            return StatusCode(500, "An error occurred while retrieving orders");
        }
    }
}