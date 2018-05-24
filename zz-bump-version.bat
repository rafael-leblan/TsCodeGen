python ./nuget/bump-version-update-patch.py
git am -3 ./nuget/.bump-version/RafaelSoft.TsCodeGen-bumpVersion.patch
git checkout --theirs .
git add *
git am --continue