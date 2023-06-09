using System.Data;

public sealed class TransactedDbConnection : IDbConnection
{
    private readonly IDbConnection _origin;
    private IDbTransaction? _transaction;

    public TransactedDbConnection(IDbConnection origin) => _origin = origin;

    public void Dispose() => _origin.Dispose();

    public IDbTransaction BeginTransaction() => _transaction = _origin.BeginTransaction();

    public IDbTransaction BeginTransaction(IsolationLevel il) => _transaction = _origin.BeginTransaction(il);

    public void ChangeDatabase(string databaseName) => _origin.ChangeDatabase(databaseName);

    public void Close() => _origin.Close();

    public IDbCommand CreateCommand()
    {
        var command = _origin.CreateCommand();

        command.Transaction = _transaction;

        return command;
    }

    public void Open() => _origin.Open();

#nullable disable
    public string ConnectionString
    {
        get => _origin.ConnectionString;
        set => _origin.ConnectionString = value;
    }
#nullable enable

    public int ConnectionTimeout
    {
        get => _origin.ConnectionTimeout;
    }

    public string Database
    {
        get => _origin.Database;
    }

    public ConnectionState State
    {
        get => _origin.State;
    }
}