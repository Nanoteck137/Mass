#include <stdio.h>
#include <stdint.h>
#include <stdbool.h>

int main(int argc, char **argv)
{
	uint32_t a = 4;
	uint32_t b = a++;
	
	printf("A: %d\n", a);
	printf("B: %d\n", b);

	return 0;
}