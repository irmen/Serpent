.PHONY: all sdist wheel install upload clean test check

all:
	@echo "targets include sdist, wheel, upload, install, clean"

sdist:
	python setup.py sdist
	@echo "Look in the dist/ directory"

wheel:
	python setup.py bdist_wheel
	@echo "Look in the dist/ directory"

upload:
	python setup.py sdist upload
	python setup.py bdist_wheel upload

install:
	python setup.py install

test:
	python -E test_serpent.py

check:
	flake8 --exclude .tox --ignore E501

clean:
	@echo "Removing tox dirs, logfiles, .pyo/.pyc files..."
	rm -rf .tox
	find . -name __pycache__ -print0 | xargs -0 rm -rf
	find . -name \*_log -print0 | xargs -0  rm -f
	find . -name \*.log -print0 | xargs -0  rm -f
	find . -name \*.pyo -print0 | xargs -0  rm -f
	find . -name \*.pyc -print0 | xargs -0  rm -f
	find . -name \*.class -print0 | xargs -0  rm -f
	find . -name \*.DS_Store -print0 | xargs -0  rm -f
	find . -name TEST-*.xml -print0 | xargs -0  rm -f
	find . -name TestResult.xml -print0 | xargs -0  rm -f
	rm -f MANIFEST
	rm -rf build
	rm -rf dotnet/Serpent/obj dotnet/Serpent.Test/obj
	rm -rf dotnet/Serpent/bin dotnet/Serpent.Test/bin
	find . -name  '.#*' -print0 | xargs -0  rm -f
	find . -name  '#*#' -print0 | xargs -0  rm -f
	@echo "clean!"
