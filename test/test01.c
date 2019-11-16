#include <stdio.h>
#include <stdint.h>
#include <stdbool.h>

int main(int argc, char **argv)
{
	float a = 3.14f;
	uint32_t b = (float)(a + 3.14f);
	float c = b;

	printf("B: %u\n", b);
	printf("C: %f\n", c);
	return 0;
}