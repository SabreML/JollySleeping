# Abandon hope all ye who enter here, for bad python code awaits!


import os, re, sys

def check_file_names():
	bad_file_names = []
	for file in os.listdir("JollySleeping/scenes/sleep screen - jollysleeping"):
		if file[-3:] != "png":
			continue

		illustration_name = os.path.splitext(file)[0]
		if check_name_format(illustration_name) == False:
			bad_file_names.append(file)

	return bad_file_names

def check_positions_file():
	bad_position_entries = []
	with open("JollySleeping/scenes/sleep screen - jollysleeping/positions.txt", "r") as positions_file:
		file_lines = positions_file.read().splitlines()
		for line in file_lines:
			# Validate the coordinates at the start.
			if not re.match(r"-?\d{1,3}, -?\d{1,3}: ", line):
				bad_position_entries.append(line)
				continue

			# Validate the illustration name list as a whole.
			if not re.search(r": ((\b(artificer|gourmand|red|rivulet|saint|spear|white|yellow)-?){2,}(, )?)+$", line):
				bad_position_entries.append(line)
				continue

			# Validate the illustration names individually.
			illustration_name_list = re.search(r": (.*)", line).group(1).split(", ")
			for illustration_name in illustration_name_list:
				if check_name_format(illustration_name) == False:
					bad_position_entries.append(line)
					continue

	return bad_position_entries


def check_name_format(illustration_name):
	# Firstly, make sure that the general formatting is correct. (Only slugcat names with dashes in between, no typos)
	if not re.fullmatch(r"((artificer|gourmand|red|rivulet|saint|spear|white|yellow)-?){2,}", illustration_name):
		return False

	# Secondly, make sure it's all in alphabetical order.
	slugcat_types = illustration_name.split("-")
	if sorted(slugcat_types) != slugcat_types:
		return False

	# Formatting is correct!
	return True

def main():
	bad_file_names = check_file_names()
	bad_position_entries = check_positions_file()

	if len(bad_file_names) == 0 and len(bad_position_entries) == 0:
		print("Illustrations successfully validated!")
	else:
		print("-- Formatting errors discovered --")
		if len(bad_file_names):
			print("Illustration file names:")
			print(*bad_file_names, sep = "\n")
		if len(bad_position_entries):
			print("Positions file:")
			print(*bad_position_entries, sep = "\n")
		sys.exit(1)

main()
