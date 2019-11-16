#include <stdio.h>
#include <stdint.h>
#include <stdbool.h>

int main(int argc, char **argv)
{
	int8_t a = 255;
	uint32_t b = (uint32_t)a;

	printf("Val: %d\n", b);

	return 0;
}