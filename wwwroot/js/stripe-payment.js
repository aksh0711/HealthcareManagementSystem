// Stripe Payment Processing JavaScript
document.addEventListener('DOMContentLoaded', function () {
    const stripePublicKey = document.getElementById('stripe-public-key')?.value;
    
    if (!stripePublicKey) {
        console.error('Stripe public key not found');
        return;
    }

    const stripe = Stripe(stripePublicKey);
    const elements = stripe.elements();

    // Card Element styles
    const cardStyle = {
        style: {
            base: {
                color: '#32325d',
                fontFamily: 'Arial, sans-serif',
                fontSmoothing: 'antialiased',
                fontSize: '16px',
                '::placeholder': {
                    color: '#aab7c4'
                }
            },
            invalid: {
                color: '#fa755a',
                iconColor: '#fa755a'
            }
        }
    };

    // Create card element
    const cardElement = elements.create('card', cardStyle);
    const cardElementContainer = document.getElementById('card-element');
    
    if (cardElementContainer) {
        cardElement.mount('#card-element');
    }

    // Handle real-time validation errors from the card Element
    const cardErrorsElement = document.getElementById('card-errors');
    cardElement.on('change', function(event) {
        if (event.error) {
            cardErrorsElement.textContent = event.error.message;
            cardErrorsElement.style.display = 'block';
        } else {
            cardErrorsElement.textContent = '';
            cardErrorsElement.style.display = 'none';
        }
    });

    // Handle form submission
    const paymentForm = document.getElementById('stripe-payment-form');
    if (paymentForm) {
        paymentForm.addEventListener('submit', async function(event) {
            event.preventDefault();
            
            const submitButton = document.getElementById('submit-payment');
            const loadingSpinner = document.getElementById('loading-spinner');
            const paymentAmount = document.getElementById('payment-amount')?.textContent || '0';

            // Disable submit button and show loading
            submitButton.disabled = true;
            submitButton.textContent = 'Processing...';
            if (loadingSpinner) {
                loadingSpinner.style.display = 'inline-block';
            }

            try {
                const { token, error } = await stripe.createToken(cardElement);

                if (error) {
                    // Show error to customer
                    cardErrorsElement.textContent = error.message;
                    cardErrorsElement.style.display = 'block';
                } else {
                    // Submit token to server
                    const tokenInput = document.createElement('input');
                    tokenInput.type = 'hidden';
                    tokenInput.name = 'stripeToken';
                    tokenInput.value = token.id;
                    paymentForm.appendChild(tokenInput);
                    
                    paymentForm.submit();
                }
            } catch (err) {
                console.error('Error creating Stripe token:', err);
                cardErrorsElement.textContent = 'An unexpected error occurred. Please try again.';
                cardErrorsElement.style.display = 'block';
            } finally {
                // Re-enable submit button
                submitButton.disabled = false;
                submitButton.textContent = `Pay ${paymentAmount}`;
                if (loadingSpinner) {
                    loadingSpinner.style.display = 'none';
                }
            }
        });
    }

    // Payment method selection handling
    const paymentMethodSelect = document.getElementById('PaymentMethodId');
    const creditCardSection = document.getElementById('credit-card-section');
    
    if (paymentMethodSelect && creditCardSection) {
        paymentMethodSelect.addEventListener('change', function() {
            const selectedOption = this.options[this.selectedIndex];
            const requiresGateway = selectedOption.dataset.requiresGateway === 'true';
            const isCredit = selectedOption.dataset.type === 'CreditCard';
            
            if (requiresGateway && isCredit) {
                creditCardSection.style.display = 'block';
            } else {
                creditCardSection.style.display = 'none';
            }
        });
        
        // Trigger change event on page load
        paymentMethodSelect.dispatchEvent(new Event('change'));
    }

    // Amount calculation and validation
    const amountInput = document.getElementById('Amount');
    const balanceAmount = parseFloat(document.getElementById('balance-amount')?.textContent || '0');
    
    if (amountInput) {
        amountInput.addEventListener('input', function() {
            const amount = parseFloat(this.value || '0');
            const submitButton = document.getElementById('submit-payment');
            
            if (amount <= 0) {
                this.setCustomValidity('Amount must be greater than 0');
                if (submitButton) submitButton.disabled = true;
            } else if (amount > balanceAmount) {
                this.setCustomValidity('Amount cannot exceed balance amount');
                if (submitButton) submitButton.disabled = true;
            } else {
                this.setCustomValidity('');
                if (submitButton) submitButton.disabled = false;
            }
        });
    }

    // Payment type handling
    const paymentTypeRadios = document.querySelectorAll('input[name="Type"]');
    const partialAmountSection = document.getElementById('partial-amount-section');
    
    paymentTypeRadios.forEach(radio => {
        radio.addEventListener('change', function() {
            if (this.value === 'Partial') {
                partialAmountSection.style.display = 'block';
                amountInput.required = true;
            } else {
                partialAmountSection.style.display = 'none';
                amountInput.required = false;
                if (this.value === 'Full') {
                    amountInput.value = balanceAmount.toFixed(2);
                }
            }
        });
    });

    // Initialize payment type on page load
    const checkedPaymentType = document.querySelector('input[name="Type"]:checked');
    if (checkedPaymentType) {
        checkedPaymentType.dispatchEvent(new Event('change'));
    }

    // Invoice item management
    const addItemButton = document.getElementById('add-item-button');
    const addItemForm = document.getElementById('add-item-form');
    
    if (addItemButton && addItemForm) {
        addItemButton.addEventListener('click', function() {
            addItemForm.style.display = addItemForm.style.display === 'none' ? 'block' : 'none';
        });
    }

    // Item total calculation
    const itemQuantity = document.getElementById('item-quantity');
    const itemUnitPrice = document.getElementById('item-unit-price');
    const itemTotal = document.getElementById('item-total');
    
    function updateItemTotal() {
        if (itemQuantity && itemUnitPrice && itemTotal) {
            const quantity = parseInt(itemQuantity.value || '0');
            const unitPrice = parseFloat(itemUnitPrice.value || '0');
            const total = quantity * unitPrice;
            itemTotal.textContent = total.toFixed(2);
        }
    }
    
    if (itemQuantity) itemQuantity.addEventListener('input', updateItemTotal);
    if (itemUnitPrice) itemUnitPrice.addEventListener('input', updateItemTotal);

    // Confirmation dialogs
    const deleteButtons = document.querySelectorAll('.delete-button');
    deleteButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            if (!confirm('Are you sure you want to delete this item?')) {
                e.preventDefault();
            }
        });
    });

    const refundButtons = document.querySelectorAll('.refund-button');
    refundButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            if (!confirm('Are you sure you want to process this refund?')) {
                e.preventDefault();
            }
        });
    });

    // Auto-refresh payment status
    const paymentStatusElement = document.getElementById('payment-status');
    if (paymentStatusElement && paymentStatusElement.dataset.status === 'Processing') {
        setTimeout(() => {
            location.reload();
        }, 3000); // Refresh after 3 seconds for processing payments
    }

    // Format currency inputs
    const currencyInputs = document.querySelectorAll('input[data-type="currency"]');
    currencyInputs.forEach(input => {
        input.addEventListener('input', function() {
            let value = this.value.replace(/[^\d.]/g, '');
            if (value.indexOf('.') !== value.lastIndexOf('.')) {
                value = value.substring(0, value.lastIndexOf('.'));
            }
            const parts = value.split('.');
            if (parts[1] && parts[1].length > 2) {
                parts[1] = parts[1].substring(0, 2);
                value = parts.join('.');
            }
            this.value = value;
        });
    });

    // Dashboard chart initialization (if Chart.js is available)
    if (typeof Chart !== 'undefined') {
        initializeDashboardCharts();
    }
});

// Dashboard charts initialization
function initializeDashboardCharts() {
    // Revenue by month chart
    const revenueChartCanvas = document.getElementById('revenue-chart');
    if (revenueChartCanvas) {
        const revenueData = JSON.parse(revenueChartCanvas.dataset.revenue || '{}');
        const ctx = revenueChartCanvas.getContext('2d');
        
        new Chart(ctx, {
            type: 'line',
            data: {
                labels: Object.keys(revenueData),
                datasets: [{
                    label: 'Revenue',
                    data: Object.values(revenueData),
                    borderColor: 'rgb(75, 192, 192)',
                    backgroundColor: 'rgba(75, 192, 192, 0.2)',
                    tension: 0.1
                }]
            },
            options: {
                responsive: true,
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function(value) {
                                return '$' + value.toLocaleString();
                            }
                        }
                    }
                }
            }
        });
    }

    // Revenue by payment method chart
    const paymentMethodChartCanvas = document.getElementById('payment-method-chart');
    if (paymentMethodChartCanvas) {
        const paymentMethodData = JSON.parse(paymentMethodChartCanvas.dataset.paymentMethods || '{}');
        const ctx = paymentMethodChartCanvas.getContext('2d');
        
        new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: Object.keys(paymentMethodData),
                datasets: [{
                    data: Object.values(paymentMethodData),
                    backgroundColor: [
                        '#FF6384',
                        '#36A2EB',
                        '#FFCE56',
                        '#4BC0C0',
                        '#9966FF',
                        '#FF9F40'
                    ]
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: {
                        position: 'bottom'
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                return context.label + ': $' + context.raw.toLocaleString();
                            }
                        }
                    }
                }
            }
        });
    }
}
