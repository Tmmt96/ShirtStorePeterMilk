using Microsoft.AspNetCore.Mvc;

namespace ShirtStore.Web.Controllers;

public class StaticPagesController : Controller
{
    [HttpGet("/terms")]
    public IActionResult Terms()
    {
        ViewData["Title"] = "Termos e condições | Peter Milk";
        ViewData["Description"] = "Consulta os termos de compra, pagamento, entrega e devolução da Peter Milk.";
        return View();
    }

    [HttpGet("/privacy")]
    public IActionResult Privacy()
    {
        ViewData["Title"] = "Política de privacidade | Peter Milk";
        ViewData["Description"] = "Consulta como a Peter Milk recolhe, utiliza e protege os dados pessoais dos seus clientes.";
        return View();
    }
}
