using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using RotAndRuin.Application.DTOs;
using RotAndRuin.Application.Services;
using RotAndRuin.Application.Interfaces;
using RotAndRuin.Application.DTOs.Requests;
using Swashbuckle.AspNetCore.Annotations;


namespace RotAndRuin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private const string CartSessionKey = "CartId";

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    private string GetOrCreateCartId()
    {
        string? cartId = HttpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(cartId))
        {
            cartId = Guid.NewGuid().ToString();
            HttpContext.Session.SetString(CartSessionKey, cartId);
        }
        return cartId;
    }

    [HttpGet("{cartId}")]
    public async Task<ActionResult<CartDto>> GetCart(string cartId)
    {
        var cart = await _cartService.GetCartAsync(cartId);
        if (cart == null)
        {
            // This is the 404 error your frontend is correctly receiving
            return NotFound(new { message = $"Cart with ID {cartId} not found." });
        }
        return Ok(cart);
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem([FromBody] AddCartItemRequest request)
    {
        var cartId = GetOrCreateCartId();
        try
        {
            var cart = await _cartService.AddItemToCartAsync(cartId, request.ProductId, request.Quantity);
            return Ok(cart);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("items/{productId}")]
    public async Task<ActionResult<CartDto>> UpdateItemQuantity(Guid productId, [FromBody] int quantity)
    {
        var cartId = GetOrCreateCartId();
        try
        {
            var cart = await _cartService.UpdateCartItemQuantityAsync(cartId, productId, quantity);
            return Ok(cart);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpDelete("items/{productId}")]
    public async Task<ActionResult<CartDto>> RemoveItem(Guid productId)
    {
        var cartId = GetOrCreateCartId();
        var cart = await _cartService.RemoveItemFromCartAsync(cartId, productId);
        return Ok(cart);
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var cartId = GetOrCreateCartId();
        await _cartService.ClearCartAsync(cartId);
        return Ok();
    }
}