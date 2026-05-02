@echo off
echo Syscalculator 2.0 Git release branches
echo.
echo Branches:
echo   daily       active development
echo   beta        tester-ready releases
echo   production  stable public releases
echo.
echo First setup:
echo   git init
echo   git add .
echo   git commit -m "Initial Syscalculator 2.0 source import"
echo   git branch -M daily
echo   git branch beta
echo   git branch production
echo.
echo Daily changelog:
echo   BUILD_FIX_CHANGELOG.bat -Channel daily -Fix "Fixed something"
echo.
echo Beta changelog:
echo   BUILD_FIX_CHANGELOG.bat -Channel beta -Fix "Fixed tested issue" -Addition "Added tester feature"
echo.
echo Production changelog:
echo   BUILD_FIX_CHANGELOG.bat -Channel production -Fix "Stability fix" -Addition "Production release"
echo.
echo Full procedure:
echo   docs\GIT_RELEASE_BRANCH_WORKFLOW.md
