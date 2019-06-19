# EventBot

## Environment variables
### `token` variable
This is variable for discord bot's token obtained from discord developer panel.
### `dbconnection` variable
This is environment variable for Mysql / MariaDb database connection. Example connection string:

```Server=localhost,123;Database=eventbot;User=root;Password=password;```


`Add-Migration InitialDatabase -Context MySqlDatabaseService -OutputDir Migrations\MySql`
`Add-Migration InitialDatabase -Context SqliteDatabaseService -OutputDir Migrations\Sqlite`