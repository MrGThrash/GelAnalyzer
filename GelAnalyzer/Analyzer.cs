﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GelAnalyzer
{
   public static class Analyzer
    {
        #region getting MolSizes
        // Находит координаты всех центров масс 
        public static double[] GetMolsSizes (int molNum, int molCount, List<double[]> file, double[] bSizes)
       {
           
           double xSize = 0.0, ySize = 0.0, zSize = 0.0;

            double xyRad = 0.0, zRad = 0.0;

               xSize = bSizes[0];
               ySize = bSizes[1];
               zSize = bSizes[2];

           var points = new List<double[]>();

           double molDiam = 0;
           int counter = 0;

           var bordermols = new List<int[]>();

           // get search radius
           for (int i = 0; i < molCount; i++)
           {
               var mol = file.Skip(i * molNum).Take(molNum).ToList();
               var diam = StructFormer.GetDiameter(mol);

               if (diam.Max() < 0.8 * Math.Max(xSize, ySize))
               {
                   molDiam += diam.Max();
                    xyRad += StructFormer.GetHydroRadius2D(mol);
                    zRad += Math.Sqrt(StructFormer.GetAxInertSquareRadius(mol,2));

                    counter++;
                   var cm = StructFormer.GetCenterMass(mol);
                   points.Add(new double[] { cm[0] , cm[1] });
               }
               else
               {
                   int sign = 0; // периодичность по оси X
                    if (diam[1] > 0.8 * ySize)
                   {
                       if (diam[0] > 0.8 * xSize)
                       {
                           sign = 2; // периодичность по осям XY
                        }
                       else
                       {
                           sign = 1; // периодичность по оси Y
                       }
                   }

                   bordermols.Add(new int[] { i, sign });
               }
           }

           molDiam /= counter;

           // Избавляемся от периодичности
           foreach (var c in bordermols)
           {
               var mol = file.Skip(c[0] * molNum).Take(molNum).ToList();
               var normMol = new List<double[]>();

               if (c[1] < 2)
               {
                   // Все частицы считаются от левого нижнего угла
                   var minCoord = mol.Min(x => x[c[1]]);
                   foreach (var d in mol)
                   {
                       if (Math.Abs(minCoord - d[c[1]]) < molDiam * 1.1)
                       {
                           normMol.Add(d);
                       }
                       else
                       {
                           if (c[1] == 0)
                               normMol.Add(new double[] { d[0] - xSize, d[1], d[2], d[3] });
                           else
                               normMol.Add(new double[] { d[0], d[1] - ySize, d[2], d[3] });
                       }
                   }
                }
               else
               {
                   var minCoord = mol.Min(x => x[0]);

                   var intermol = new List<double[]>();

                   // shift by x
                   foreach (var d in mol)
                   {
                       if (Math.Abs(minCoord - d[0]) < molDiam * 1.1)
                       {
                           intermol.Add(d);
                       }
                       else
                       {
                           intermol.Add(new double[] { d[0] - xSize, d[1], d[2], d[3] });
                       }
                   }

                   foreach (var d in intermol)
                   {
                       if (Math.Abs(minCoord - d[1]) < molDiam * 1.1)
                       {
                           normMol.Add(d);
                       }
                       else
                       {
                           normMol.Add(new double[] { d[0], d[1] - ySize, d[2], d[3] });
                       }
                   }
               }
            
                xyRad += StructFormer.GetHydroRadius2D(normMol);
                zRad += StructFormer.GetAxInertSquareRadius(normMol, 2);
            }


            xyRad /= molCount;
            zRad /= molCount;

           return new double[] { xyRad, zRad };
       }
        #endregion

        #region SurfCoverage
        public static double GetSurfCoverage (string filename)
        {
           
                    var file = new List<double[]>();
                    double xSize = 0, ySize = 0, zSize = 0;
                
                        file = FileWorker.LoadConfLines(filename);

                        xSize = Math.Abs(file[file.Count - 1][0] - file[file.Count - 8][0]);
                        ySize = Math.Abs(file[file.Count - 1][1] - file[file.Count - 8][1]);
                        zSize = Math.Abs(file[file.Count - 1][2]);
                    
                    // Finding interface coordinate

                    int liqCount = file.Where(x => x[3].Equals(1.03) || x[3].Equals(1.02)).ToList().Count;
                    int solvCount = file.Where(x => x[3].Equals(1.02)).ToList().Count;

                    double zInterCoord = zSize * ((double)solvCount / (double)liqCount);

                    // Lattice calc
                    int surCounter = 0;

                    for (int i = 1; i <= (int)xSize; i++)
                    {
                        for (int j = 1; j <= (int)ySize; j++)
                        {
                            double coef1 = xSize / 2.0, coef2 = ySize / 2.0;

                            var cell = file.Where(x => Math.Abs(x[2] - zInterCoord) <= 1.0
                                                      && x[0] >= (i - 1 - coef1) && x[0] <= (i - coef1)
                                                      && x[1] >= (j - 1 - coef2) && x[1] <= (j - coef2)).ToList();

                            int unitPol = cell.Where(x => x[3].Equals(1.00) || x[3].Equals(1.01)
                                                     || x[3].Equals(1.04) || x[3].Equals(1.05)).ToList().Count;

                            double cellFrac = (double)unitPol / (double)cell.Count();

                            if (cellFrac >= (double)(1.0 / 3.0))
                            {
                                surCounter++;
                            }
                        }
                    }

                    double surfFrac = Math.Round((double)surCounter / (xSize * ySize), 3);

            return surfFrac;
           
        }
        #endregion

        #region AutoCenter
        public static void DoAutoCenter(bool withZCenter, int k, double[] sizes, double[] centerPoint, 
            List<double[]> file)
        {
            if (k == 0)
            {
                return;
            }

            for (int i = 0; i < k; i++)
            {
                double[] centerCoord = StructFormer.CenterStructure(centerPoint, file);

                if (Math.Abs(centerCoord[0]) < 0.5 && Math.Abs(centerCoord[1]) < 0.5 && Math.Abs(centerCoord[2]) < 0.5)
                {
                    break;
                }

                int breakmark = 0;

                if (!withZCenter)
                {
                    centerCoord[2] = 0.0;
                }

                MolData.ShiftAllDouble(3, sizes, centerCoord, centerPoint, file);

                double[] diam = StructFormer.GetDiameter(file);

                for (int j = 0; j <= 2; j++)
                {
                    if (Math.Abs(diam[j] - sizes[j]) <= 2)
                    {
                        var shifts = new double[3];
                        shifts[j] = -StructFormer.CenterAxis_Type2(false, j, sizes[j], centerPoint[j], file);

                        if (j == 2 && !withZCenter)
                        {
                            shifts[j] = 0.0;
                        }
                        MolData.ShiftAllDouble(3, sizes, shifts, centerPoint, file);
                    }
                    else
                    {
                        if (centerCoord[j] < 0.5)
                        {
                            breakmark++;
                        }
                    }
                }

                if ((breakmark == 2 && !withZCenter) || (breakmark == 3 && withZCenter))
                {
                    break;
                }
            }
        }

        #endregion

        public static double GetDistance(double x1, double x2, double y1, double y2, double z1, double z2)
        {
            double distance = Math.Sqrt(Math.Pow((x2 - x1), 2) + Math.Pow((y2 - y1), 2) + Math.Pow((z2 - z1), 2));

            return distance;
        }

        #region get cross-sections in microgel from box
        public static List<double[]> GetCrossSections(List<double[]> list)
        {
            List<double[]> crossSections = new List<double[]>();
            List<double[]> temp = new List<double[]>();
            foreach (var c in list)
            {

                temp = list.FindAll(x => ((Analyzer.GetDistance(x[0], c[0], x[1], c[1], x[2], c[2]) < 0.5)));
               if (temp.Count == 3)
                {
                    crossSections.Add(c);
                }
            }
            
            return crossSections;
        }
        #endregion
    }
}