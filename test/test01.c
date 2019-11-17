#include <stdio.h>
#include <stdint.h>
#include <stdbool.h>
#include <stdlib.h>

int main(int argc, char **argv)
{
	uint32_t *a = malloc(8);
	a[0] = 123;

	free(a);

	return 0;
}