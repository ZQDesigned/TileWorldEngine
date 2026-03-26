# GitHub Pages Publishing

This repository now includes a DocFX-based GitHub Pages workflow in:

```text
.github/workflows/docs.yml
```

## What The Workflow Does

On pushes to `master` or `main`, manual dispatches, and pull requests, the workflow will:

1. restore the pinned DocFX tool,
2. build the documentation source assemblies,
3. run DocFX against `docs/docfx.json`,
4. upload the generated `docs/_site` output,
5. deploy that artifact to GitHub Pages on non-PR runs.

## Repository Configuration Required

GitHub Pages still needs to be enabled in the repository settings.

For this repository, open:

1. `GitHub -> ZQDesigned/TileWorldEngine -> Settings`
2. `Pages`
3. Under `Build and deployment`, set `Source` to `GitHub Actions`

After that, push to the default branch or manually run the `Docs` workflow from the `Actions` tab.

## Expected Site URL

If the repository stays under the current owner/name, the GitHub Pages site URL should be:

```text
https://zqdesigned.github.io/TileWorldEngine/
```

## Notes

- No extra deployment secret is required for the standard GitHub Pages workflow.
- The current environment does not have GitHub CLI available, so the repository-side Pages setting still needs to be enabled from the GitHub web UI.
- If you later rename the default branch or move the repository, update the workflow trigger branches and the expected site URL accordingly.
