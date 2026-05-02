export namespace MockDataHelper {
  export function getRandomInt(min: number, max: number): number {
    min = Math.ceil(min);
    max = Math.floor(max);
    return Math.floor(Math.random() * (max - min + 1)) + min;
  }

  export function getRandomDate(start: Date, end: Date): Date {
    return new Date(start.getTime() + Math.random() * (end.getTime() - start.getTime()));
  }

  export function getRandomDecimal(min: number, max: number, decimalPlaces: number): string {
    const rand = Math.random() * (max - min) + min;
    const power = Math.pow(10, decimalPlaces);
    return (Math.floor(rand * power) / power).toString();
  }

  export function getRandomString(length: number): string {
    const characters = 'abcdefghijklmnopqrstuvwxyz';
    let result = '';

    for (let i = 0; i < length; i++) {
      const randomIndex = Math.floor(Math.random() * characters.length);
      result += characters[randomIndex];
    }

    return result;
  }

  export function getRandomBoolean(): boolean {
    return Math.random() < 0.5;
  }
}
