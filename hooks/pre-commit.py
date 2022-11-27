import os
import sys
import subprocess
import re

# we can use path relative from the repo
def git_diff(filepath):
	return subprocess.call(['git', 'diff', '--cached', '--exit-code', '--quiet', filepath])
def git_add(filepath):
	return subprocess.call(['git', 'add', filepath])

# get the span of the version number
# we dont use a xml library because it changes the order/formatting and there is no way to solve it
def get_version_span(text):
	start = text.find('<Metadata>')
	if start == -1:
		raise Exception('Cannot find version in manifest')
	start = start + len('<Metadata>')
	end = text.find('</Metadata>', start)
	if end == -1:
		raise Exception('Cannot find version in manifest')
	s = text[start:end]
	m = re.search('<Identity\s*(?:\s*\w+\s*=\s*"[^"]*"\s*)*Version\s*=\s*"(\d+\.\d+\.\d+\.\d+)"\s*', s)
	if m == None:
		raise Exception('Cannot find version in manifest')
	return (m.span(1)[0] + start, m.span(1)[1] + start)

def read_file(filepath):
	with open(filepath, 'r') as f:
		return f.read()

def write_file(filepath, text):
	with open(filepath, 'w') as f:
		f.write(text)
		f.flush()

# version is in the form 1.0.0.4
def increment_version(version_text):
  parts = version_text.split('.')
  parts[-1] = str(int(parts[-1]) + 1)
  return '.'.join(parts)

manifest_path = r'vhdl4vs.ProjectType\source.extension.vsixmanifest'

if git_diff(manifest_path) == 0:
	text = read_file(manifest_path)
	ver_span = get_version_span(text)
	ver = text[ver_span[0]:ver_span[1]]
	ver = increment_version(ver)
	text = text[:ver_span[0]] + ver + text[ver_span[1]:]
	write_file(manifest_path, text)
	git_add(manifest_path)
	
sys.exit(0)