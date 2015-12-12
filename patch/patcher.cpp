#include <cassert>
#include <vector>
#include <memory>

// patch find difference between src_file and patched_file and applies the diff to dest_file

const char* src_file = "D:\\World of Warcraft Beta\\binaries\\20796\\WowB-64.exe";
const char* patched_file = "D:\\World of Warcraft Beta\\binaries\\20796\\WowB-64__.exe";
const char* dest_file = "D:\\World of Warcraft Beta\\binaries\\20796\\WowB.exe";
const char* output_file = "D:\\World of Warcraft Beta\\binaries\\20796\\WowB_.exe";

const int max_patch_size = 256;

struct patch {
	size_t location;
	size_t patch_size;
	char src[max_patch_size];
	char dest[max_patch_size];
	size_t find_size;
	size_t find_location;
};

size_t open_file(const char* file_name, std::unique_ptr<char[]>* contents)
{
	FILE* fp = fopen(file_name, "rb");
	assert(fp != NULL);
	fseek(fp, 0L, SEEK_END);
	size_t file_size = ftell(fp);
	fseek(fp, 0L, SEEK_SET);
	contents->reset(new char[file_size]);
	fread(contents->get(), sizeof(char), file_size, fp);
	fclose(fp);
	return file_size;
}

void find_differences(const char* src, const char* patched, size_t size, std::vector<patch>* patchs)
{
	patch p; int j = 0;
	for (size_t i = 0; i < size; i += (j + 1) , j = 0)
	{
		p.patch_size = 0;
		while (src[i + j] != patched[i + j])
		{
			assert(j < max_patch_size);
			p.src[j] = src[i + j];
			p.dest[j] = patched[i + j];
			p.patch_size = ++j;
		}
		if (p.patch_size > 0)
		{
			p.find_size = p.patch_size;
			p.location = i;
			p.find_location = i;
			patchs->push_back(p);
		}
	}
}

size_t find_first(const char* contents, const char* str, const size_t contents_size, const size_t str_size)
{
	for (size_t i = 0; i <= contents_size - str_size; ++i)
	{
		if (memcmp(contents + i, str, str_size) == 0)
			return i;
	}
	return contents_size;
}

int find_count(const char* contents, const char* str, const size_t contents_size, const size_t str_size)
{
	int nfound = 0;
	for (size_t i = 0; i <= contents_size - str_size; ++i)
	{
		if (memcmp(contents + i, str, str_size) == 0)
			++nfound;
	}
	return nfound;
}

patch find_unique(patch p, const char* file_contents, size_t file_size)
{
	bool should_extend = false;
	while (true)
	{
		int cnt = find_count(file_contents, file_contents + p.find_location, file_size, p.find_size);
		assert(cnt >= 1);
		if (cnt == 1)
			break;
		++p.find_size;
		if (should_extend && p.find_location > 0)
			--p.find_location;
		should_extend = !should_extend;
	}
	return p;
}

bool apply_patch(char* output, const char* target, const char* original, const patch& p, size_t target_size)
{
	size_t patch_location = find_first(target, original + p.find_location, target_size, p.find_size);
	if (patch_location >= target_size)
		return false;
	size_t patch_offset = p.location - p.find_location;
	memcpy(output + patch_location + patch_offset, p.dest, p.patch_size);
	return true;
}

int main()
{
	std::vector<patch> patch_map;
	std::unique_ptr<char[]> src_file_contents;
	std::unique_ptr<char[]> patched_file_contents;
	size_t size;
	bool equal_size = (size = open_file(src_file, &src_file_contents)) == open_file(patched_file, &patched_file_contents);
	assert(equal_size);
	find_differences(src_file_contents.get(), patched_file_contents.get(), size, &patch_map);
	patched_file_contents.release();

	std::unique_ptr<char[]> target_file_contents;
	std::unique_ptr<char[]> output_file_contents;
	size_t target_size = open_file(dest_file, &target_file_contents);
	output_file_contents.reset(new char[target_size]);
	memcpy(output_file_contents.get(), target_file_contents.get(), target_size);

	for (auto p : patch_map) 
	{
		patch unique_patch = find_unique(p, src_file_contents.get(), size);
		if (!apply_patch(output_file_contents.get(), target_file_contents.get(),
			src_file_contents.get(), unique_patch, target_size))
		{
			printf("patch not found at %x\n", p.location);
		}
		printf(".");
	}

	FILE* fp = fopen(output_file, "wb");
	fwrite(output_file_contents.get(), target_size, 1, fp);
	fclose(fp);

	return 0;
}