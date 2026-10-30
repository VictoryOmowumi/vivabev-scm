# start / stop the database (data is kept)
docker compose -f infra/local/docker-compose.yml up -d
docker compose -f infra/local/docker-compose.yml stop

# see SQL Server's log
docker logs vivabev-sql --tail 50

# wipe everything and start fresh (deletes all data)
docker compose -f infra/local/docker-compose.yml down -v

# build and test the whole solution
dotnet build
dotnet test