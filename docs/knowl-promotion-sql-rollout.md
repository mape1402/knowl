# KnOwl Promotion SQL Rollout

## Ordered Scripts

Apply pending promotion/runtime scripts in this order when updating an existing KnOwl database:

1. `013_20260725195512_AddContractLifecycleState.sql`
2. `014_20260725200621_AddContractArtifacts.sql`
3. `015_20260725201049_AddContractReleases.sql`
4. `016_20260725201635_AddRuntimeContractArtifacts.sql`

For a full guarded script, use:

- `000_full-idempotent-knowl-migrations.sql`

## Runtime Notes

- KnOwl does not execute EF migrations on application startup.
- Apply SQL before rolling out the application image.
- Existing Event and Command versions are initialized as `Draft`.
- Existing Events and Commands are initialized as active records.
- Runtime storage only stores deployed Event/Command artifacts; it does not execute contracts.
