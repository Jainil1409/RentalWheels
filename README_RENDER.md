# Deploying to Render

Quick steps to deploy this app on Render using the included `Dockerfile` and `render.yaml`.

1. Add repository to Render and connect your Git provider.
2. In Render dashboard, create the web service from this repo or use the `render.yaml` manifest.
3. Add environment variable `ConnectionStrings__DefaultConnection` (Secret) with the Postgres connection string:

   Host=dpg-d6ntptcr85hc7380bv10-a.oregon-postgres.render.com;Port=5432;Database=vehiclerentaldb_0cnu;Username=vehiclerentaldb_0cnu_user;Password=<password>

   Or set `DATABASE_URL` if you prefer the URL form.

4. Deploy the web service. After the image builds, run the migration job (from `render.yaml`) once via the Render dashboard:

   - Go to the Jobs section and trigger `migrate-database` (this runs `dotnet vehicle_management_system_mvc.dll --migrate`).

5. Confirm the web service becomes healthy and open the site.

Notes
- Do not commit secrets to the repo. Use Render's dashboard to store connection strings.
- The `--migrate` job runs migrations and exits; normal web startup also attempts migrations but logs errors instead of crashing.
