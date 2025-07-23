using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RotAndRuin.Application.DTOs;
using RotAndRuin.Application.Services;
using RotAndRuin.Application.Interfaces;
using RotAndRuin.Domain.Entities.Order;
using Stripe;
using Stripe.Checkout;
using Session = Stripe.Checkout.Session;

namespace RotAndRuin.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;
    private readonly string _whSecret;
    private readonly ICartService _cartService;
    

    public PaymentsController(
        ICartService _cartService,
        IPaymentService paymentService,
        ILogger<PaymentsController> logger,
        IConfiguration config)
    {
        _cartService = _cartService;
        _paymentService = paymentService;
        _logger = logger;
        _whSecret = config["Stripe:WhSecret"];
    }

    [Authorize]
    [HttpPost("create-checkout-session")]
    public async Task<ActionResult<string>> CreateCheckoutSession(
        [FromQuery] string cartId,
        [FromBody] ShippingAddressDto shippingAddress)
    {
        try
        {
            if (string.IsNullOrEmpty(cartId))
            {
                return BadRequest(new { error = "Cart ID is required" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation($"Creating checkout session for user: {userId}, cart: {cartId}");

            if (shippingAddress == null)
            {
                return BadRequest(new { error = "Shipping address is required" });
            }

            var sessionUrl = await _paymentService.CreateCheckoutSession(cartId, userId, shippingAddress);
            return Ok(sessionUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session for cart {CartId}", cartId);
            return StatusCode(500, new { error = "Failed to create checkout session", details = ex.Message });
        }
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _whSecret
            );

            _logger.LogInformation($"Received Stripe webhook event: {stripeEvent.Type}");

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                _logger.LogInformation($"Processing completed checkout session: {session.Id}");

                try
                {
                    await _paymentService.HandleWebhook(json, Request.Headers["Stripe-Signature"]);
                    return Ok();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing webhook");
                    return StatusCode(500);
                }
            }

            return Ok();
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Error constructing Stripe event");
            return BadRequest();
        }
    }
    
    
    
    [HttpPost("create-payment-intent")]
    public async Task<ActionResult<PaymentIntentDto>> CreatePaymentIntent(
        [FromBody] string cartId)
    {
        try
        {
            if (string.IsNullOrEmpty(cartId))
            {
                return BadRequest(new { error = "Cart ID is required" });
            }

            var paymentIntent = await _paymentService.CreateOrUpdatePaymentIntent(cartId);
            return Ok(paymentIntent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent");
            return BadRequest(new { error = ex.Message });
        }
    }
    
    // [HttpPost("webhook")]
    // public async Task<ActionResult> StripeWebhook()
    // {
    //     var json = await new StreamReader(Request.Body).ReadToEndAsync();
    //     var stripeEvent = EventUtility.ConstructEvent(
    //         json,
    //         Request.Headers["Stripe-Signature"],
    //         _whSecret
    //     );
    //
    //     PaymentIntent intent;
    //     Order order;
    //
    //     switch (stripeEvent.Type)
    //     {
    //         case "payment_intent.succeeded":
    //             intent = (PaymentIntent)stripeEvent.Data.Object;
    //             order = await _paymentService
    //                 .UpdateOrderPaymentSucceeded(intent.Id);
    //             _logger.LogInformation("Payment succeeded: ", intent.Id);
    //             break;
    //         case "payment_intent.payment_failed":
    //             intent = (PaymentIntent)stripeEvent.Data.Object;
    //             _logger.LogInformation("Payment failed: ", intent.Id);
    //             break;
    //     }
    //
    //     return new EmptyResult();
    // }
}