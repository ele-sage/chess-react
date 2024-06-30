# Use the .NET SDK image for development
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dev

WORKDIR /app


COPY src/ChessAPI/*.csproj ./src/ChessAPI/
WORKDIR /app/src/ChessAPI/
RUN dotnet restore

COPY src/ChessAPI/. .

EXPOSE 5000

CMD ["dotnet", "watch", "run", "--urls", "http://0.0.0.0:5000"]
