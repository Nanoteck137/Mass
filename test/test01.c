#include <stdio.h>

int main(int argc, char **argv)
{
	FILE *file = fopen("test.txt", "rt");
	fclose(file);
	return 0;
}