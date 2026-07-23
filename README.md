# User Secrets

Перейдите в папку ASP.NET Core проекта:

```bash
cd ItransitionCourseProject
```

## PostgreSQL

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=fakedindb;Username=postgres;Password=YOUR_PASSWORD"
```

На Render эта же настройка называется `ConnectionStrings__DefaultConnection` и должна содержать строку Npgsql:

```text
Host=YOUR_HOST;Port=5432;Database=YOUR_DATABASE;Username=YOUR_USER;Password=YOUR_PASSWORD;SSL Mode=Require
```

## Google OAuth

```bash
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_GOOGLE_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_GOOGLE_CLIENT_SECRET"
```

## Facebook OAuth

```bash
dotnet user-secrets set "Authentication:Facebook:ClientId" "YOUR_FACEBOOK_APP_ID"
dotnet user-secrets set "Authentication:Facebook:ClientSecret" "YOUR_FACEBOOK_APP_SECRET"
```

## Cloudinary

```bash
dotnet user-secrets set "Cloudinary:CloudName" "YOUR_CLOUD_NAME"
dotnet user-secrets set "Cloudinary:ApiKey" "YOUR_CLOUDINARY_API_KEY"
dotnet user-secrets set "Cloudinary:ApiSecret" "YOUR_CLOUDINARY_API_SECRET"
```

## Первый администратор

```bash
dotnet user-secrets set "Seed:Admin:Email" "admin@example.com"
dotnet user-secrets set "Seed:Admin:Password" "YOUR_ADMIN_PASSWORD"
```

## Посмотреть сохранённые secrets

```bash
dotnet user-secrets list
```

## Удалить один secret

```bash
dotnet user-secrets remove "SECRET_NAME"
```

## Удалить все secrets проекта

```bash
dotnet user-secrets clear
```

Не добавляйте реальные пароли, Client Secret и API Secret в Git.
