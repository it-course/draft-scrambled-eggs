using devrating.entity;
using devrating.factory;
using devrating.sqlite;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

public sealed class RatingSystem
{
    private readonly ILoggerFactory _log;
    private readonly Formula _formula;
    private readonly Database _database;

    public RatingSystem(ILoggerFactory log, Formula formula, string database)
    {
        _log = log;
        _formula = formula;
        _database = new SqliteDatabase(
            new TransactedDbConnection(
                new SqliteConnection($"Data Source='{database}'")
            )
        );
    }

    public DateTimeOffset? LastWorkCreatedAt(string organization, string repository)
    {
        var logger = _log.CreateLogger<RatingSystem>();
        _database.Instance().Connection().Open();

        using var transaction = _database.Instance().Connection().BeginTransaction();

        try
        {
            if (!_database.Instance().Present())
                return null;

            return _database.Entities()
            .Works()
            .GetOperation()
            .Last(
                organization: organization,
                repository: repository,
                after: DateTimeOffset.MinValue
            )
            .FirstOrDefault()
            ?.CreatedAt();
        }
        finally
        {
            transaction.Rollback();
            _database.Instance().Connection().Close();
        }
    }

    public void Apply(Diff diff)
    {
        var logger = _log.CreateLogger<RatingSystem>();

        logger.LogInformation(
            new EventId(1461486),
            $"Applying diff: `{diff.ToJson()}`");

        _database.Instance().Connection().Open();

        using var transaction = _database.Instance().Connection().BeginTransaction();

        try
        {
            if (!_database.Instance().Present())
            {
                logger.LogInformation(
                    new EventId(1824416),
                    $"The DB is not present. Creating");

                _database.Instance().Create();
            }

            var authorFactory = new DefaultAuthorFactory(_database.Entities().Authors());
            var ratings = _database.Entities().Ratings();

            diff.NewWork(
               new DefaultFactories(
                   authorFactory,
                   new DefaultWorkFactory(
                       _database.Entities().Works(),
                       ratings,
                       authorFactory),
                   new DefaultRatingFactory(
                       _log,
                       authorFactory,
                       ratings,
                       _formula
                   )
               )
            );

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();

            throw;
        }
        finally
        {
            _database.Instance().Connection().Close();
        }
    }
}