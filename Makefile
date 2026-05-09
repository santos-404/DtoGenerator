.PHONY: release

BUMP ?= patch

release:
	@git diff-index --quiet HEAD -- || (echo "error: commit your changes before releasing" && exit 1)
	@[ "$$(git rev-parse --abbrev-ref HEAD)" = "main" ] || (echo "error: releases must be made from main" && exit 1)
	@tag=$$(git describe --tags --abbrev=0 2>/dev/null || echo "v0.0.0"); \
	 current=$$(echo "$$tag" | sed 's/^v//'); \
	 major=$$(echo "$$current" | cut -d. -f1); \
	 minor=$$(echo "$$current" | cut -d. -f2); \
	 patch=$$(echo "$$current" | cut -d. -f3); \
	 case "$(BUMP)" in \
	   major) major=$$((major+1)); minor=0; patch=0 ;; \
	   minor) minor=$$((minor+1)); patch=0 ;; \
	   patch) patch=$$((patch+1)) ;; \
	   *) echo "error: BUMP must be patch, minor, or major" && exit 1 ;; \
	 esac; \
	 next="v$$major.$$minor.$$patch"; \
	 echo "$$current → $$next ($(BUMP))"; \
	 git tag "$$next" && git push origin "$$next" && echo "Tagged. GitHub Actions will handle the rest."
