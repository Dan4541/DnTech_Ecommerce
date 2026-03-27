using Stripe;

namespace DnTech_Ecommerce.Services
{
    public class StripeService
    {
        private readonly ILogger<StripeService> _logger;
        private readonly string _secretKey;

        public StripeService(IConfiguration configuration, ILogger<StripeService> logger)
        {
            _logger = logger;
            _secretKey = configuration["Stripe:SecretKey"] ?? throw new Exception("Stripe SecretKey not configured");
            
            StripeConfiguration.ApiKey = _secretKey;
            
            _logger.LogInformation("Stripe Service initialized");
        }

        /// <summary>
        /// Crea un PaymentIntent en Stripe
        /// </summary>
        public async Task<(bool success, string clientSecret, string paymentIntentId, string message)> CreatePaymentIntent(
            decimal amount, 
            string currency = "usd",
            string customerEmail = "")
        {
            try
            {
                _logger.LogInformation($"Creating Stripe PaymentIntent for {amount} {currency}");

                // Stripe maneja cantidades en centavos
                var amountInCents = (long)(amount * 100);

                var options = new PaymentIntentCreateOptions
                {
                    Amount = amountInCents,
                    Currency = currency.ToLower(),
                    PaymentMethodTypes = new List<string> { "card" },
                    ReceiptEmail = string.IsNullOrEmpty(customerEmail) ? null : customerEmail,
                    Description = "Compra en TechStore"
                };

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                _logger.LogInformation($"PaymentIntent created: {paymentIntent.Id}");

                return (true, paymentIntent.ClientSecret, paymentIntent.Id, "PaymentIntent creado exitosamente");
            }
            catch (StripeException ex)
            {
                _logger.LogError($"Stripe error: {ex.Message}");
                return (false, string.Empty, string.Empty, $"Error de Stripe: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating PaymentIntent: {ex.Message}");
                return (false, string.Empty, string.Empty, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Confirma que el pago fue exitoso
        /// </summary>
        public async Task<(bool success, string transactionId, string message)> ConfirmPayment(string paymentIntentId)
        {
            try
            {
                _logger.LogInformation($"Confirming payment: {paymentIntentId}");

                var service = new PaymentIntentService();
                var paymentIntent = await service.GetAsync(paymentIntentId);

                if (paymentIntent.Status == "succeeded")
                {
                    _logger.LogInformation($"Payment confirmed: {paymentIntent.Id}");
                    
                    // Obtenemos el ID del cargo exitoso.
                    var chargeId = paymentIntent.LatestChargeId ?? paymentIntent.Id;
                    
                    return (true, chargeId, "Pago completado exitosamente");
                }

                _logger.LogWarning($"Payment status: {paymentIntent.Status}");
                return (false, string.Empty, $"Estado del pago: {paymentIntent.Status}");
            }
            catch (StripeException ex)
            {
                _logger.LogError($"Stripe error confirming payment: {ex.Message}");
                return (false, string.Empty, $"Error de Stripe: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error confirming payment: {ex.Message}");
                return (false, string.Empty, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Cancela un PaymentIntent (si aún no se completó)
        /// </summary>
        public async Task<bool> CancelPaymentIntent(string paymentIntentId)
        {
            try
            {
                var service = new PaymentIntentService();
                await service.CancelAsync(paymentIntentId);
                
                _logger.LogInformation($"PaymentIntent cancelled: {paymentIntentId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cancelling PaymentIntent: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Crea un reembolso
        /// </summary>
        public async Task<(bool success, string refundId, string message)> CreateRefund(string chargeId, decimal? amount = null)
        {
            try
            {
                _logger.LogInformation($"Creating refund for charge: {chargeId}");

                var options = new RefundCreateOptions
                {
                    Charge = chargeId
                };

                if (amount.HasValue)
                {
                    options.Amount = (long)(amount.Value * 100); // En centavos
                }

                var service = new RefundService();
                var refund = await service.CreateAsync(options);

                _logger.LogInformation($"Refund created: {refund.Id}");
                return (true, refund.Id, "Reembolso procesado exitosamente");
            }
            catch (StripeException ex)
            {
                _logger.LogError($"Stripe error creating refund: {ex.Message}");
                return (false, string.Empty, $"Error de Stripe: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating refund: {ex.Message}");
                return (false, string.Empty, $"Error: {ex.Message}");
            }
        }
    }
}