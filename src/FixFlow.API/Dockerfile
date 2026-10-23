# ============ المرحلة 1: البناء (SDK) ============
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1) ننسخ ملفات الـ csproj بس ونعمل restore.
#    ليه؟ عشان Docker يحفظ الخطوة دي (Cache)، ومنحمّلش الباكدجات تاني
#    إلا لو الباكدجات نفسها اتغيرت.
COPY src/FixFlow.Domain/FixFlow.Domain.csproj src/FixFlow.Domain/
COPY src/FixFlow.Application/FixFlow.Application.csproj src/FixFlow.Application/
COPY src/FixFlow.Infrastructure/FixFlow.Infrastructure.csproj src/FixFlow.Infrastructure/
COPY src/FixFlow.API/FixFlow.API.csproj src/FixFlow.API/
RUN dotnet restore src/FixFlow.API/FixFlow.API.csproj

# 2) بعدين ننسخ باقي الكود ونعمل publish
COPY src/ src/
RUN dotnet publish src/FixFlow.API/FixFlow.API.csproj -c Release -o /app/publish --no-restore

# ============ المرحلة 2: التشغيل (Runtime خفيف) ============
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
LABEL org.opencontainers.image.source="https://github.com/Awed-karim/FixFlow"
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FixFlow.API.dll"]