using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using ShirtStore.Domain.Entities;

namespace ShirtStore.Web.Models;

public class CheckoutViewModel
{
    [BindNever]
    [ValidateNever]
    public Cart Cart { get; set; } = null!;

    [Required(ErrorMessage = "Indica o teu nome completo.")]
    [Display(Name = "Nome completo")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indica o teu email.")]
    [EmailAddress(ErrorMessage = "Indica um email válido.")]
    [Display(Name = "Email")]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indica o teu telemóvel.")]
    [Phone(ErrorMessage = "Indica um número de telemóvel válido.")]
    [Display(Name = "Telemóvel")]
    public string CustomerPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indica a morada de entrega.")]
    [Display(Name = "Morada")]
    public string ShippingAddressLine1 { get; set; } = string.Empty;

    [Display(Name = "Complemento")]
    public string? ShippingAddressLine2 { get; set; }

    [Required(ErrorMessage = "Indica a localidade.")]
    [Display(Name = "Localidade")]
    public string ShippingCity { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indica o código postal.")]
    [Display(Name = "Código postal")]
    public string ShippingPostalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indica o país.")]
    [Display(Name = "País")]
    public string ShippingCountry { get; set; } = "Portugal";

    [Display(Name = "NIF")]
    public string? TaxId { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Aceita os termos e condições para continuar.")]
    public bool AcceptTerms { get; set; }

    [BindNever]
    public decimal SubTotal { get; set; }

    [BindNever]
    public decimal Shipping { get; set; }

    [BindNever]
    public decimal Total { get; set; }
}