#include <stdio.h>

void *test(void *ptr)
{
	char *p = (char *)ptr;

	return p + 4;
}

int main(int argc, char **argv)
{
	FILE *file = fopen("Wooh.txt", "wt");
	fwrite("Test Wooh Content", 8, 1, file);
	fclose(file);
	return 0;
}