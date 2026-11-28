using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Orleans;
using StellarDotnetSdk;
using System.Linq;
using Utilities.Billing.Api.Protos;
using Utilities.Billing.Api.Services;
using Utilities.Billing.Contracts;
using Utilities.Billing.Data;
using Utilities.Billing.Data.Entities;
using Utilities.Billing.StellarWallets;

namespace Utilities.Billing.Api.GrpcServices;


/// <summary>
/// gRPC implementation of the billing <c>StellarService</c> API, also exposed as JSON/REST through gRPC transcoding
/// under <c>/v1/...</c>. Every call needs the <c>RequireScope</c> authorization policy and delegates to <see cref="ITenantService"/>.
/// </summary>
[Authorize(Policy = "RequireScope")]
public class StellarService : Protos.StellarService.StellarServiceBase
{
    private readonly ITenantService _tenantService;

    /// <summary>Creates the service.</summary>
    /// <param name="tenantService">Tenant-scoped billing logic that every call delegates to.</param>
    public StellarService(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    /// <summary>Registers a tenant (management company). <c>POST /v1/tenants</c>.</summary>
    /// <param name="request">Tenant name and its Stellar account (wallet) ID.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The new tenant's ID (UUID string).</returns>
    public override async Task<AddTenantResponse> AddTenant(AddTenantRequest request, ServerCallContext context)
    {
        var reply = await _tenantService.AddTenant(new AddTenantCommand
        {
            Name = request.Name,
            Account = request.Account
        });

        return new AddTenantResponse { Id = reply.Id.ToString() };
    }

    /// <summary>Changes the name and Stellar account of a registered tenant. <c>PUT /v1/tenants</c>.</summary>
    /// <param name="request">Tenant ID (UUID string) and the new name and account.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>An empty response.</returns>
    public override async Task<UpdateTenantResponse> UpdateTenant(UpdateTenantRequest request, ServerCallContext context)
    {
        await _tenantService.UpdateTenant(new UpdateTenantCommand
        {
            TenantId = request.Id,
            Name = request.Name,
            Account = request.Account
        });
        return new UpdateTenantResponse();
    }

    /// <summary>Registers a consumed resource (electricity, water, ...) as a Stellar asset of the tenant. <c>POST /v1/assets</c>.</summary>
    /// <param name="request">Tenant ID, asset code, issuer account ID and the equipment model codes that consume it.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The new asset's ID (UUID string).</returns>
    public override async Task<AddAssetResponse> AddAsset(AddAssetRequest request, ServerCallContext context)
    {
        var command = new AddAssetCommand
        {
            TenantId = request.TenantId,
            AssetCode = request.AssetCode,
            Issuer = request.Issuer,
        };
        command.ModelCodes.Add(request.ModelCodes);

        var reply = await _tenantService.AddAsset(command);

        return new AddAssetResponse { AssetId = reply.Id.ToString() };
    }

    /// <summary>Gets a tenant's asset. <c>GET /v1/assets/{assetId}</c>.</summary>
    /// <param name="request">Tenant ID and asset ID (UUID strings).</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Asset code, issuer, the system (master) account to send tokens to, and equipment model codes.</returns>
    public override async Task<GetAssetResponse> GetAsset(GetAssetRequest request, ServerCallContext context)
    {
        var command = new GetAssetCommand
        {
            TenantId = request.TenantId,
            Id = request.AssetId,
        };

        var reply = await _tenantService.GetAsset(command);

        var response = new GetAssetResponse
        {
            AssetId = reply.Id.ToString(),
            AssetCode = reply.Code,
            IssuerAccount = reply.IssuerAccount,
            MasterAccount = reply.MasterAccount,
        };
        response.ModelCodes.Add(reply.ModelCodes);

        return response;
    }

    /// <summary>Sets the equipment model codes of a tenant's asset (codes not in the request are removed). <c>PUT /v1/assets</c>.</summary>
    /// <param name="request">Tenant ID, asset ID and the new model codes.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>An empty response.</returns>
    public override async Task<UpdateAssetResponse> UpdateAsset(UpdateAssetRequest request, ServerCallContext context)
    {
        var command = new UpdateAssetCommand
        {
            TenantId = request.TenantId,
            Id = request.AssetId,
        };
        command.ModelCodes.Add(request.ModelCodes);

        await _tenantService.UpdateAsset(command);

        return new UpdateAssetResponse { };
    }

    /// <summary>Lists a tenant's assets, one page at a time. <c>GET /v1/assets</c>.</summary>
    /// <param name="request">Tenant ID and optional offset and limit; unset values use the service defaults.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The page of assets and the total count.</returns>
    public override async Task<ListAssetsResponse> ListAssets(ListAssetsRequest request, ServerCallContext context)
    {
        var command = new ListAssetsCommand {
            TenantId = request.TenantId,
            Offset = request.HasOffset ? (int)request.Offset : default(int?),
            Limit = request.HasLimit ? (int)request.Limit : default(int?),
        };

        var reply = await _tenantService.ListAssets(command);
        var response = new ListAssetsResponse { Total = reply.Total };
        foreach (var item in reply.Items)
        {
            response.Items.Add(new ListAssetsResponse.Types.AssetsItem
            {
                AssetId = item.Id.ToString(),
                AssetCode = item.Code,
                IssuerAccount = item.Issuer,
            });
        }

        return response;
    }

    /// <summary>Creates a meter (customer) account for a device input, managed by the system. <c>POST /v1/accounts</c>.</summary>
    /// <param name="request">Tenant ID, asset ID, device serial, input code, and whether to create a muxed (M-) account instead of a separate G- account.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The new customer account's ID (UUID string).</returns>
    public override async Task<CreateCustomerAccountResponse> CreateCustomerAccount(CreateCustomerAccountRequest request, ServerCallContext context)
    {
        var command = new CreateCustomerAccountCommand
        {
            TenantId = request.TenantId,
            AssetId = request.AssetId,
            DeviceSerial = request.DeviceSerial,
            InputCode = request.InputCode,
            CreateMuxed = request.CreateMuxed,
        };

        var reply = await _tenantService.CreateCustomerAccount(command);

        return new CreateCustomerAccountResponse { CustomerAccountId = reply.AccountId.ToString() };
    }

    /// <summary>Gets a meter (customer) account. <c>GET /v1/accounts/{customerAccountId}</c>.</summary>
    /// <param name="request">Tenant ID and customer account ID (UUID strings).</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The account's wallet, asset, issuer, system (master) account and state name.</returns>
    public override async Task<GetCustomerAccountResponse> GetCustomerAccount(GetCustomerAccountRequest request, ServerCallContext context)
    {
        var command = new GetCustomerAccountCommand
        {
            TenantId = request.TenantId,
            CustomerAccountId = request.CustomerAccountId,
        };

        var reply = await _tenantService.GetCustomerAccount(command);

        return new GetCustomerAccountResponse
        {
            CustomerAccountId = reply.Id.ToString(),
            CustomerAccount = reply.Wallet,
            AssetId = reply.AssetId.ToString(),
            Asset = reply.AssetCode,
            Issuer = reply.AssetIssuer,
            MasterAccount = reply.MasterAccount,
            State = reply.State.ToString(),
        };
    }

    /// <summary>Lists a tenant's meter (customer) accounts, one page at a time. <c>GET /v1/accounts</c>.</summary>
    /// <param name="request">Tenant ID and optional offset and limit; unset values use the service defaults.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The page of accounts and the total count.</returns>
    public override async Task<ListCustomerAccountsResponse> ListCustomerAccounts(ListCustomerAccountsRequest request, ServerCallContext context)
    {
        var command = new ListCustomerAccountsCommand {
            TenantId = request.TenantId,
            Offset = request.HasOffset ? (int)request.Offset : default(int?),
            Limit = request.HasLimit ? (int)request.Limit : default(int?),
        };

        var reply = await _tenantService.ListCustomerAccounts(command);
        var response = new ListCustomerAccountsResponse { Total = reply.Total };
        foreach(var item in reply.Items)
        {
            response.Items.Add(new ListCustomerAccountsResponse.Types.AccountsItem
            {
                CustomerAccountId = item.Id.ToString(),
                CustomerAccount = item.Wallet,
                Asset = item.AssetCode,
                DeviceSerial = item.DeviceSerial,
                InputCode = item.InputCode,
                State = item.State.ToString(),
            });
        }

        return response;
    }

    /// <summary>Removes a meter (customer) account. <c>DELETE /v1/accounts</c>. The account must be in the <c>Ok</c> state; it is marked <c>Deleting</c> and removed from Stellar in the background.</summary>
    /// <param name="request">Tenant ID and customer account ID (UUID strings).</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>An empty response.</returns>
    public override async Task<DeleteCustomerAccountResponse> DeleteCustomerAccount(DeleteCustomerAccountRequest request, ServerCallContext context)
    {
        var command = new DeleteCustomerAccountCommand
        {
            TenantId = request.TenantId,
            CustomerAccountId = request.CustomerAccountId,
        };

        await _tenantService.DeleteCustomerAccount(command);

        return new DeleteCustomerAccountResponse { };
    }

    /// <summary>Creates an invoice for resource consumption as a transaction pre-signed by the system. <c>POST /v1/invoices</c>.</summary>
    /// <param name="request">Tenant ID, customer account ID, payer's Stellar account and the amount (invariant-culture decimal string, non-zero).</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The base64-encoded XDR envelope of the invoice transaction.</returns>
    public override async Task<CreateInvoiceResponse> CreateInvoice(CreateInvoiceRequest request, ServerCallContext context)
    {
        var command = new CreateInvoiceCommand
        {
            TenantId = request.TenantId,
            CustomerAccountId = request.CustomerAccountId,
            PayerAccount = request.PayerAccount,
            Amount = request.Amount,
        };

        var reply = await _tenantService.CreateInvoice(command);

        return new CreateInvoiceResponse
        {
            InvoiceXdr = reply.Xdr,
        };
    }


    /// <summary>Lists invoices of a meter account for a period. <c>GET /v1/invoices</c>.</summary>
    /// <param name="request">Tenant ID, customer account ID, and the period as Unix timestamps in seconds (inclusive).</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The invoices with their amount, XDR and whether they are completed.</returns>
    public override async Task<ListInvoicesResponse> ListInvoices(ListInvoicesRequest request, ServerCallContext context)
    {
        var command = new ListInvoicesCommand
        {
            TenantId = request.TenantId,
            CustomerAccountId = request.CustomerAccountId,
            PeriodFrom = request.PeriodFrom,
            PeriodTo = request.PeriodTo,
        };

        var reply = await _tenantService.ListInvoices(command);

        var response = new ListInvoicesResponse();
        foreach (var item in reply.Items)
        {
            response.Items.Add(new ListInvoicesResponse.Types.InvoicesListItem
            {
                TransactionId = item.TransactionId,
                Amount = item.Amount,
                Xdr = item.Xdr,
                Processed = item.Processed,
            });
        }

        return response;
    }
}

