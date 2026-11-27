
# Stage 1：編譯 .NET 10 Worker
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY . .

RUN dotnet publish \
    "BIOHub.AnalysisWorker/BIOHub.AnalysisWorker.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Stage 2：取得 ASP.NET Core Runtime
# 因為 Worker 目前參考 BIOHub MVC 專案
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Stage 3：以先前成功的 DESeq2 Image 為基礎
FROM biohub-r:dev AS final

COPY --from=runtime /usr/share/dotnet /usr/share/dotnet

ENV DOTNET_ROOT=/usr/share/dotnet
ENV PATH="/usr/share/dotnet:${PATH}"
ENV DOTNET_RUNNING_IN_CONTAINER=true

WORKDIR /app

COPY --from=build /app/publish .
# 將 R 分析腳本複製到 Image
COPY BIOHub.AnalysisWorker/RScripts/ /app/RScripts/
ENTRYPOINT ["dotnet", "BIOHub.AnalysisWorker.dll"]
