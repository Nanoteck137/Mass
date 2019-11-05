#include <stdio.h>

int a[4] = { [1] = 1, 2, 3, };

void Test()
{
	printf("Hello World\n");
}

int main(int argc, char** argv)
{
	unsigned int x = 0;
	if (x >= 4)
	{
		printf("Wooh");
	}
	else {
		printf("Test");
	}

	return 0;
}