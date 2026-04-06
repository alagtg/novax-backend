using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YourProject.API.DTOs.Billing;
using YourProject.API.DTOs.Common;
using YourProject.API.Services;

namespace YourProject.API.Controllers;

[Authorize(Roles = "ADMIN,COMPTABLE_FACTURE")]
public class BillingController : BaseApiController
{
    private readonly BillingService _billing;

    public BillingController(BillingService billing)
    {
        _billing = billing;
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
        => Ok(await _billing.GetSettings());

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] SaveBillingSettingsRequest req)
        => Ok(await _billing.UpdateSettings(req));

    [HttpPost("settings/logo")]
    public async Task<IActionResult> UploadLogo([FromForm] IFormFile file)
        => Ok(await _billing.UploadLogo(file));

    [HttpPost("settings/template")]
    public async Task<IActionResult> UploadTemplate([FromForm] IFormFile file)
        => Ok(await _billing.UploadTemplate(file));

    [HttpGet("invoices")]
    public async Task<ActionResult<PagedResult<InvoiceDto>>> GetInvoices(
        [FromQuery] int year,
        [FromQuery] int? month,
        [FromQuery] int? dossierId,
        [FromQuery] string? q,
        [FromQuery] PagedQuery paged)
    {
        var res = await _billing.Search(year, month, dossierId, q, paged);
        return Ok(res);
    }

    [HttpPost("invoices/import-excel")]
    public async Task<ActionResult<List<InvoiceDto>>> ImportExcel(
        [FromForm] int year,
        [FromForm] int month,
        [FromForm] IFormFile file)
    {
        try
        {
            var res = await _billing.ImportWorkbookInvoices(year, month, file);
            return Ok(res);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message,
                detail = ex.GetBaseException().Message
            });
        }
    }

    [HttpGet("invoices/{id}")]
    public async Task<IActionResult> GetInvoice(int id)
    {
        var invoice = await _billing.GetById(id);
        return invoice == null ? NotFound() : Ok(invoice);
    }

    [HttpPost("invoices")]
    public async Task<IActionResult> CreateInvoice([FromBody] SaveInvoiceRequest req)
        => Ok(await _billing.Create(req));

    [HttpPut("invoices/{id}")]
    public async Task<IActionResult> UpdateInvoice(int id, [FromBody] SaveInvoiceRequest req)
    {
        var invoice = await _billing.Update(id, req);
        return invoice == null ? NotFound() : Ok(invoice);
    }

    [HttpPut("invoices/{id}/payment")]
    public async Task<IActionResult> UpdatePayment(int id, [FromBody] UpdateInvoicePaymentRequest req)
    {
        var invoice = await _billing.UpdatePayment(id, req);
        return invoice == null ? NotFound() : Ok(invoice);
    }

    [HttpPost("invoices/{id}/pdf")]
    public async Task<IActionResult> UploadInvoicePdf(int id, [FromForm] IFormFile file)
    {
        var invoice = await _billing.UploadInvoicePdf(id, file);
        return invoice == null ? NotFound() : Ok(invoice);
    }

    [HttpPost("invoices/{id}/send-email")]
    public async Task<IActionResult> SendInvoiceEmail(int id)
    {
        var ok = await _billing.SendInvoiceEmail(id);
        return ok
            ? Ok(new { message = "Email envoyé." })
            : BadRequest(new { message = "Email impossible." });
    }

    [HttpPost("invoices/send-all-emails")]
    public async Task<IActionResult> SendAllEmails([FromBody] SendAllInvoicesEmailRequest req)
        => Ok(new { count = await _billing.SendAllInvoiceEmails(req.Year, req.Month) });

    [HttpGet("annual-client-table")]
    public async Task<IActionResult> AnnualClientTable([FromQuery] int year, [FromQuery] string? q)
        => Ok(await _billing.GetAnnualClientTable(year, q));

    [HttpDelete("invoices/{id}")]
    public async Task<IActionResult> DeleteInvoice(int id)
    {
        var ok = await _billing.Delete(id);
        return ok ? Ok(new { message = "Supprimé." }) : NotFound();
    }
    [HttpDelete("invoices/delete-month")]
    public async Task<IActionResult> DeleteMonth(
    [FromQuery] int year,
    [FromQuery] int month)
    {
        var count = await _billing.DeleteMonth(year, month);
        return Ok(new { count });
    }
    [HttpGet("invoices/{id}/pdf")]
    public async Task<IActionResult> Pdf(int id)
    {
        var bytes = await _billing.GeneratePdf(id);
        if (bytes == null) return NotFound();

        return File(bytes, "application/pdf", $"facture_{id}.pdf");
    }
}