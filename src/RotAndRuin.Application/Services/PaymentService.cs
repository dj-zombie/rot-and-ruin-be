

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RotAndRuin.Application.DTOs;
using RotAndRuin.Application.Interfaces;
using RotAndRuin.Domain.Entities.Order;
using Stripe;
using Stripe.Checkout;

namespace RotAndRuin.Application.Services;

public class PaymentService : IPaymentService
{
    // private readonly IConfiguration _config;
    private readonly IApplicationDbContext _context;
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;
    private ILogger<PaymentService> _logger;

    public PaymentService(
        ILogger<PaymentService> logger,
        IConfiguration config,
        IApplicationDbContext context,
        ICartService cartService, 
        IOrderService orderService)
    {
        // _config = config;
        _context = context;
        _cartService = cartService;
        _orderService = orderService;
        _logger = logger;
        
        var stripeKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
        if (string.IsNullOrEmpty(stripeKey))
        {
            throw new Exception("Stripe secret key not configured");
        }
    
        StripeConfiguration.ApiKey = stripeKey;
    }
    
    
    public async Task<string> CreateCheckoutSession(string cartId, string userId, ShippingAddressDto shippingAddress)
{
    try 
    {
        _logger.LogInformation($"Attempting to create checkout session for cart: {cartId}");
        
        var cart = await _cartService.GetCartAsync(cartId);
        
        if (cart == null)
        {
            _logger.LogError($"Cart not found: {cartId}");
            throw new ArgumentException($"Cart not found: {cartId}");
        }
        
        if (cart.Items == null || !cart.Items.Any())
        {
            _logger.LogError($"Cart is empty: {cartId}");
            throw new ArgumentException("Cart is empty");
        }

        _logger.LogInformation($"Creating checkout session for cart {cartId} with {cart.Items.Count} items. Total: {cart.Total}");

        // Create the line items list
        var lineItems = new List<SessionLineItemOptions>();
        
        foreach (var item in cart.Items)
        {
            _logger.LogInformation(
                $"Adding item to Stripe session: " +
                $"Name={item.ProductName}, " +
                $"Price={item.Price}, " +
                $"Quantity={item.Quantity}, " +
                $"SubTotal={item.SubTotal}, " +
                $"ShippingTotal={item.ShippingTotal}, " +
                $"Total={item.Total}");

            // Add the product cost
            lineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)(item.Price * 100), // Convert to cents
                    Currency = "usd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = item.ProductName,
                        Description = $"Includes shipping: ${item.ShippingPrice:F2}",
                        Images = new List<string> 
                        { 
                            !string.IsNullOrEmpty(item.OriginalUrl) 
                                ? item.OriginalUrl 
                                : "https://rotandruin.com/placeholder.jpg"
                        }
                    }
                },
                Quantity = item.Quantity
            });

            // Add shipping as a separate line item if you want it itemized
            if (item.ShippingPrice > 0)
            {
                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.ShippingPrice * 100),
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Shipping for {item.ProductName}",
                        }
                    },
                    Quantity = item.Quantity
                });
            }
        }

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = lineItems,
            Mode = "payment",
            SuccessUrl = Environment.GetEnvironmentVariable("CLIENT_URL") + "/checkout/success?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = Environment.GetEnvironmentVariable("CLIENT_URL") + "/checkout/cancelled",
            ShippingAddressCollection = new SessionShippingAddressCollectionOptions
            {
                AllowedCountries = new List<string> { "US" }
            },
            Metadata = new Dictionary<string, string>
            {
                { "CartId", cartId },
                { "UserId", userId ?? "anonymous" },
                { "ShippingFullName", shippingAddress.FullName },
                { "ShippingAddress1", shippingAddress.AddressLine1 },
                { "ShippingAddress2", shippingAddress.AddressLine2 ?? "" },
                { "ShippingCity", shippingAddress.City },
                { "ShippingState", shippingAddress.State },
                { "ShippingPostal", shippingAddress.PostalCode },
                { "ShippingCountry", shippingAddress.Country },
                { "OrderTotal", cart.Total.ToString("F2") }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);
        
        // Update cart with Stripe session details
        cart.PaymentIntentId = session.PaymentIntentId;
        cart.ClientSecret = session.ClientSecret;
        await _cartService.UpdateCartAsync(cart);
        
        _logger.LogInformation($"Successfully created Stripe session: {session.Id}");
        
        return session.Url;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error creating checkout session for cart {cartId}");
        throw;
    }
}

    public async Task HandleWebhook(string json, string stripeSignature)
    {
        try
        {
            _logger.LogInformation("Handling Stripe webhook");
        
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                stripeSignature,
                Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET")
            );

            _logger.LogInformation($"Webhook Event Type: {stripeEvent.Type}");

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                _logger.LogInformation($"Processing completed session: {session.Id}");

                if (session == null)
                {
                    throw new Exception("Invalid session object received");
                }

                var shippingAddress = new ShippingAddressDto
                {
                    FullName = session.Metadata["ShippingFullName"],
                    AddressLine1 = session.Metadata["ShippingAddress1"],
                    AddressLine2 = session.Metadata["ShippingAddress2"],
                    City = session.Metadata["ShippingCity"],
                    State = session.Metadata["ShippingState"],
                    PostalCode = session.Metadata["ShippingPostal"],
                    Country = session.Metadata["ShippingCountry"]
                };

                await _orderService.CreateOrderFromCartAsync(
                    session.Metadata["CartId"],
                    session.Metadata["UserId"],
                    shippingAddress
                );

                _logger.LogInformation($"Successfully processed order for session: {session.Id}");
            }
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Stripe webhook error");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "General webhook error");
            throw;
        }
    }
    
    

    public async Task<PaymentIntentDto> CreateOrUpdatePaymentIntent(string cartId)
    {
        var cart = await _cartService.GetCartAsync(cartId);
        if (cart == null)
        {
            throw new ArgumentException($"Cart not found with ID: {cartId}");
        }

        var service = new PaymentIntentService(); // Remove _stripeClient

        PaymentIntent intent;

        try 
        {
            if (string.IsNullOrEmpty(cart.PaymentIntentId))
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(cart.Total * 100),
                    Currency = "usd",
                    PaymentMethodTypes = new List<string> { "card" }
                };

                intent = await service.CreateAsync(options);
            
                // Update cart using CartService
                cart.PaymentIntentId = intent.Id;
                cart.ClientSecret = intent.ClientSecret;
                await _cartService.UpdateCartAsync(cart);
            }
            else
            {
                var options = new PaymentIntentUpdateOptions
                {
                    Amount = (long)(cart.Total * 100)
                };
            
                intent = await service.UpdateAsync(
                    cart.PaymentIntentId, 
                    options
                );
            
                // Update cart using CartService
                cart.ClientSecret = intent.ClientSecret;
                await _cartService.UpdateCartAsync(cart);
            }

            return new PaymentIntentDto
            {
                ClientSecret = intent.ClientSecret,
                PaymentIntentId = intent.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating/updating payment intent for cart {cartId}");
            throw;
        }
    }

    public async Task<Order> UpdateOrderPaymentSucceeded(string paymentIntentId)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.PaymentIntentId == paymentIntentId);

        if (order == null) throw new Exception("Order not found");

        order.Status = OrderStatus.PaymentReceived;
        await _context.SaveChangesAsync();

        return order;
    }
}