using System.Globalization;
using System.Net;
using System.Text;
using ZimMarket.Application.Common.Models;

namespace ZimMarket.Infrastructure.Notifications;

/// <summary>
/// HTML transactional emails for SendGrid. Layout is table-based for broad client support.
/// </summary>
public static class TransactionalEmailTemplates
{
    private const string BrandName = "ZimMarket";

    public static EmailMessage Welcome(string to, string recipientDisplayName)
    {
        string name = HtmlEncode(recipientDisplayName);
        string body = WrapBody(
            "Welcome",
            $"""
            <p>Hi {name},</p>
            <p>Thanks for joining {BrandName}. You can now browse sellers, place orders, and track deliveries from your account.</p>
            <p>If you did not create this account, please contact support.</p>
            """);

        return new EmailMessage
        {
            To = to,
            Subject = $"Welcome to {BrandName}",
            Body = body,
            IsHtml = true
        };
    }

    public static EmailMessage KycApproved(string to, string recipientDisplayName)
    {
        string name = HtmlEncode(recipientDisplayName);
        string body = WrapBody(
            "KYC approved",
            $"""
            <p>Hi {name},</p>
            <p>Your identity verification (KYC) has been <strong>approved</strong>. Your account permissions are now updated accordingly.</p>
            <p>You can continue using {BrandName} with full access for your role.</p>
            """);

        return new EmailMessage
        {
            To = to,
            Subject = $"{BrandName}: KYC approved",
            Body = body,
            IsHtml = true
        };
    }

    public static EmailMessage KycRejected(string to, string recipientDisplayName, string? reason)
    {
        string name = HtmlEncode(recipientDisplayName);
        string reasonBlock = string.IsNullOrWhiteSpace(reason)
            ? "<p>We could not approve your submission with the documents provided.</p>"
            : $"<p><strong>Reason:</strong> {HtmlEncode(reason)}</p>";

        string body = WrapBody(
            "KYC update",
            $"""
            <p>Hi {name},</p>
            <p>Your identity verification (KYC) was <strong>not approved</strong> at this time.</p>
            {reasonBlock}
            <p>You may resubmit corrected documents from your account when you are ready.</p>
            """);

        return new EmailMessage
        {
            To = to,
            Subject = $"{BrandName}: KYC update",
            Body = body,
            IsHtml = true
        };
    }

    public static EmailMessage OrderConfirmation(
        string to,
        string recipientDisplayName,
        string orderReference,
        string totalSummary)
    {
        string name = HtmlEncode(recipientDisplayName);
        string order = HtmlEncode(orderReference);
        string total = HtmlEncode(totalSummary);
        string body = WrapBody(
            "Order confirmation",
            $"""
            <p>Hi {name},</p>
            <p>Thank you for your order. We have received it and will keep you updated.</p>
            <table role="presentation" cellpadding="8" cellspacing="0" style="margin:16px 0;border-collapse:collapse;">
              <tr><td style="border:1px solid #e5e7eb;"><strong>Order</strong></td><td style="border:1px solid #e5e7eb;">{order}</td></tr>
              <tr><td style="border:1px solid #e5e7eb;"><strong>Total</strong></td><td style="border:1px solid #e5e7eb;">{total}</td></tr>
            </table>
            <p>You will receive another email when your order status changes.</p>
            """);

        return new EmailMessage
        {
            To = to,
            Subject = $"{BrandName}: Order {orderReference} confirmed",
            Body = body,
            IsHtml = true
        };
    }

    public static EmailMessage DeliveryNotification(
        string to,
        string recipientDisplayName,
        string orderReference,
        string detailLine)
    {
        string name = HtmlEncode(recipientDisplayName);
        string order = HtmlEncode(orderReference);
        string detail = HtmlEncode(detailLine);
        string body = WrapBody(
            "Delivery update",
            $"""
            <p>Hi {name},</p>
            <p>There is an update on your delivery for order <strong>{order}</strong>.</p>
            <p style="margin:16px 0;padding:12px;background:#f3f4f6;border-radius:6px;">{detail}</p>
            <p>Open {BrandName} to see live tracking and next steps.</p>
            """);

        return new EmailMessage
        {
            To = to,
            Subject = $"{BrandName}: Delivery update for order {orderReference}",
            Body = body,
            IsHtml = true
        };
    }

    private static string WrapBody(string heading, string innerHtml)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="utf-8"/><meta name="viewport" content="width=device-width,initial-scale=1"/></head>
            <body style="margin:0;padding:0;background:#f9fafb;font-family:Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#111827;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f9fafb;padding:24px 12px;">
                <tr>
                  <td align="center">
                    <table role="presentation" width="600" cellpadding="0" cellspacing="0" style="max-width:600px;background:#ffffff;border-radius:8px;overflow:hidden;border:1px solid #e5e7eb;">
                      <tr>
                        <td style="padding:20px 24px;background:#0f766e;color:#ffffff;font-size:20px;font-weight:600;">{BrandName}</td>
                      </tr>
                      <tr>
                        <td style="padding:8px 24px 0;font-size:14px;color:#6b7280;">{HtmlEncode(heading)}</td>
                      </tr>
                      <tr>
                        <td style="padding:16px 24px 28px;font-size:15px;line-height:1.55;">{innerHtml}</td>
                      </tr>
                      <tr>
                        <td style="padding:16px 24px;background:#f9fafb;font-size:12px;color:#6b7280;border-top:1px solid #e5e7eb;">This message was sent by {BrandName}. Please do not reply if this inbox is unmonitored.</td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string HtmlEncode(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return WebUtility.HtmlEncode(text.Trim());
    }
}
