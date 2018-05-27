python ./nuget/bump-version-update-patch.py
git am -3 --ignore-space-change --ignore-whitespace ./nuget/.bump-version/RafaelSoft.TsCodeGen-bumpVersion.patch
git checkout --theirs .
git add *
git am --continue