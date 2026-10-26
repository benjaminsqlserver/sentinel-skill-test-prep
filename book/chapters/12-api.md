# Chapter 12 · The API Surface

## 12.1 The composition root

<!-- include: src/Sentinel.Api/Program.cs -->

Program.cs refuses to start outside Development. The authentication scheme
below trusts a header; that is acceptable on a laptop and nowhere else, so
the application fails loudly rather than running insecurely by accident.

<!-- include: src/Sentinel.Api/appsettings.json -->

## 12.2 Results become HTTP in one place

<!-- include: src/Sentinel.Api/Http/ResultHttpExtensions.cs -->

Every endpoint ends in `.ToOk()`, `.ToNoContent()` or `.ToCreated(...)`.
The mapping from `ErrorType` to status code exists once, so `NotFound` can
never be a 404 in one endpoint and a 400 in another. Responses are RFC 9457
problem details, with the stable error `code` as an extension member that
clients can switch on.

## 12.3 Development authentication

<!-- include: src/Sentinel.Api/Authentication/DemoUserAuthenticationHandler.cs -->

<!-- include: src/Sentinel.Api/Authentication/SentinelClaims.cs -->

The handler distinguishes *no header* (`NoResult`: anonymous, which some
endpoints allow) from *a header naming nobody* (`Fail`: 401). Endpoints read
only the `sentinel:user_id` claim, so replacing this scheme with JWT bearer
changes nothing else.

## 12.4 Guarding endpoints with permissions

<!-- include: src/Sentinel.Api/Authentication/PermissionFilter.cs -->

Hiding a menu item is user experience. This filter is security: it asks the
same domain rules, through the dispatcher, on every request.

## 12.5 The endpoint modules

One static class per feature, one `Map...` method each:

<!-- include: src/Sentinel.Api/Endpoints/MeEndpoints.cs -->

<!-- include: src/Sentinel.Api/Endpoints/UserEndpoints.cs -->

The roles, pages and sign-in modules follow the same pattern and are in the
repository. Read and change permissions are separate (`admin.users` versus
`admin.users.manage`), which is why the auditor can open the user list but
gets a 403 when creating a user.

## 12.6 Trying it by hand

<!-- include: src/Sentinel.Api/Sentinel.http -->

Run these with the API started. The second sign-in request is a preview of
Chapter 22: send a timestamp with no zone and see what the server does with
it.
