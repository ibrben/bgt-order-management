using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BGT.Order.Infrastructure.Repositories;

public sealed class OrderRepository
{
    private readonly string _connectionString;

    public OrderRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not configured.");
    }

    public async Task<bool> CustomerExistsAsync(SqlConnection connection, SqlTransaction transaction, int customerId, CancellationToken cancellationToken)
    {
        var command = new SqlCommand(
            "SELECT CASE WHEN EXISTS (SELECT 1 FROM Customers WHERE CustomerId = @CustomerId) THEN 1 ELSE 0 END",
            connection,
            transaction);
        command.Parameters.Add("@CustomerId", System.Data.SqlDbType.Int).Value = customerId;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null && Convert.ToInt32(result) == 1;
    }

    public async Task<ProductSnapshot?> GetProductAsync(SqlConnection connection, SqlTransaction transaction, int productId, CancellationToken cancellationToken)
    {
        var command = new SqlCommand(
            "SELECT ProductId, ProductName, Price, StockQuantity, IsActive FROM Products WHERE ProductId = @ProductId",
            connection,
            transaction);
        command.Parameters.Add("@ProductId", System.Data.SqlDbType.Int).Value = productId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ProductSnapshot(
            reader.GetInt32(reader.GetOrdinal("ProductId")),
            reader.GetString(reader.GetOrdinal("ProductName")),
            reader.GetDecimal(reader.GetOrdinal("Price")),
            reader.GetInt32(reader.GetOrdinal("StockQuantity")),
            reader.GetBoolean(reader.GetOrdinal("IsActive")));
    }

    public async Task<bool> DeductStockAsync(SqlConnection connection, SqlTransaction transaction, int productId, int quantity, CancellationToken cancellationToken)
    {
        var command = new SqlCommand(
            "UPDATE Products SET StockQuantity = StockQuantity - @Quantity WHERE ProductId = @ProductId AND IsActive = 1 AND StockQuantity >= @Quantity",
            connection,
            transaction);
        command.Parameters.Add("@Quantity", System.Data.SqlDbType.Int).Value = quantity;
        command.Parameters.Add("@ProductId", System.Data.SqlDbType.Int).Value = productId;

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected == 1;
    }

    public async Task<long> CreateOrderAsync(SqlConnection connection, SqlTransaction transaction, int customerId, decimal totalAmount, CancellationToken cancellationToken)
    {
        var command = new SqlCommand(
            "INSERT INTO Orders (CustomerId, OrderStatus, TotalAmount, CreatedAt, UpdatedAt) OUTPUT INSERTED.OrderId VALUES (@CustomerId, @OrderStatus, @TotalAmount, SYSUTCDATETIME(), SYSUTCDATETIME())",
            connection,
            transaction);
        command.Parameters.Add("@CustomerId", System.Data.SqlDbType.Int).Value = customerId;
        command.Parameters.Add("@OrderStatus", System.Data.SqlDbType.VarChar, 30).Value = "Pending";
        command.Parameters.Add("@TotalAmount", System.Data.SqlDbType.Decimal, 18).Value = totalAmount;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result ?? throw new InvalidOperationException("Order creation failed."));
    }

    public async Task CreateOrderItemAsync(SqlConnection connection, SqlTransaction transaction, long orderId, int productId, int quantity, decimal unitPrice, CancellationToken cancellationToken)
    {
        var command = new SqlCommand(
            "INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice) VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice)",
            connection,
            transaction);
        command.Parameters.Add("@OrderId", System.Data.SqlDbType.BigInt).Value = orderId;
        command.Parameters.Add("@ProductId", System.Data.SqlDbType.Int).Value = productId;
        command.Parameters.Add("@Quantity", System.Data.SqlDbType.Int).Value = quantity;
        command.Parameters.Add("@UnitPrice", System.Data.SqlDbType.Decimal, 18).Value = unitPrice;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public string ConnectionString => _connectionString;

    public sealed record ProductSnapshot(int ProductId, string ProductName, decimal Price, int StockQuantity, bool IsActive);
}
